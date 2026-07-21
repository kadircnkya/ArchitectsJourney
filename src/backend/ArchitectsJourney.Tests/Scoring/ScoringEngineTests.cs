using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Application.Events;
using ArchitectsJourney.Application.Events.System;
using ArchitectsJourney.Application.Models;
using ArchitectsJourney.Domain.Aggregates;
using ArchitectsJourney.Domain.Entities;
using ArchitectsJourney.Domain.Enums;
using ArchitectsJourney.Domain.ValueObjects;
using ArchitectsJourney.Engines.Scoring;
using ArchitectsJourney.Infrastructure.EventTracker;
using ArchitectsJourney.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ArchitectsJourney.Tests.Scoring;

public class ScoringEngineTests
{
    private class StubEventBus : IEventBus
    {
        public List<DomainEvent> PublishedEvents { get; } = new();

        public Task<PublicationResult> PublishAsync(DomainEvent @event, CancellationToken cancellationToken = default)
        {
            PublishedEvents.Add(@event);
            return Task.FromResult(new PublicationResult { Accepted = true, EventId = @event.EventId, SequenceNumber = (ulong)PublishedEvents.Count });
        }

        public void RegisterPublisher(PublisherRegistration registration) { }
        public void RegisterSubscriber(SubscriptionRegistration registration) { }
    }

    private class StubScoreCalculator : IScoreCalculator
    {
        public EvaluationResult Calculate(Playthrough playthrough, EvaluationOptions options)
        {
            return new EvaluationResult
            {
                TotalScore = 500,
                Rank = MissionRank.Silver,
                Passed = true,
                MissionResult = MissionResult.Success,
                Comments = []
            };
        }
    }

    private readonly ScoringEngine _sut;
    private readonly StubEventBus _eventBusStub;
    private readonly InMemoryPlaythroughRepository _playthroughRepo;
    private readonly StubScoreCalculator _scoreCalculatorStub;
    private readonly InMemoryEventIdempotencyTracker _tracker;

    public ScoringEngineTests()
    {
        _eventBusStub = new StubEventBus();
        _playthroughRepo = new InMemoryPlaythroughRepository();
        _scoreCalculatorStub = new StubScoreCalculator();
        _tracker = new InMemoryEventIdempotencyTracker();

        var options = new EvaluationOptions();

        _sut = new ScoringEngine(
            new NullLogger<ScoringEngine>(),
            _eventBusStub,
            _playthroughRepo,
            _scoreCalculatorStub,
            _tracker,
            options
        );
    }

    [Fact]
    public async Task OnEventAsync_ValidMissionCompleted_CalculatesScoreAndPublishesEvents()
    {
        // Arrange
        var ptId = Guid.NewGuid();
        var pt = new Playthrough(ptId, "m1");
        await _playthroughRepo.SaveAsync(pt);

        var evt = new MissionCompletedEvent
        {
            SessionId = Guid.NewGuid(),
            PlaythroughId = ptId,
            MissionId = "m1",
            CorrelationId = Guid.NewGuid(),
            CausationId = Guid.NewGuid()
        };

        // Act
        var result = await _sut.OnEventAsync(evt);

        // Assert
        Assert.True(result.Acknowledged);
        
        var updatedPt = await _playthroughRepo.GetByIdAsync(ptId);
        Assert.True(updatedPt!.EvaluationCompleted);
        Assert.Equal(500, updatedPt.CurrentScore);
        Assert.Equal(MissionRank.Silver, updatedPt.FinalRank);

        Assert.Equal(3, _eventBusStub.PublishedEvents.Count);
        Assert.IsType<ScoreCalculatedEvent>(_eventBusStub.PublishedEvents[0]);
        Assert.IsType<RankAssignedEvent>(_eventBusStub.PublishedEvents[1]);
        Assert.IsType<MissionEvaluationCompletedEvent>(_eventBusStub.PublishedEvents[2]);
    }

    [Fact]
    public async Task OnEventAsync_DuplicateEvent_IsIgnored()
    {
        // Arrange
        var ptId = Guid.NewGuid();
        var pt = new Playthrough(ptId, "m1");
        await _playthroughRepo.SaveAsync(pt);

        var evt = new MissionCompletedEvent
        {
            EventId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            PlaythroughId = ptId,
            MissionId = "m1",
            CorrelationId = Guid.NewGuid(),
            CausationId = Guid.NewGuid()
        };

        // Act
        var r1 = await _sut.OnEventAsync(evt);
        var r2 = await _sut.OnEventAsync(evt);

        // Assert
        Assert.True(r1.Acknowledged);
        Assert.True(r2.Acknowledged);
        Assert.Equal(3, _eventBusStub.PublishedEvents.Count); // Second run didn't publish anything
    }

    private class ThrowingScoreCalculator : IScoreCalculator
    {
        public EvaluationResult Calculate(Playthrough playthrough, EvaluationOptions options)
        {
            throw new InvalidOperationException("Calculation failed");
        }
    }

    [Fact]
    public async Task OnEventAsync_CalculationFailure_DoesNotMutateOrPublish()
    {
        var ptId = Guid.NewGuid();
        var pt = new Playthrough(ptId, "m1");
        await _playthroughRepo.SaveAsync(pt);

        var evt = new MissionCompletedEvent
        {
            SessionId = Guid.NewGuid(),
            PlaythroughId = ptId,
            MissionId = "m1",
            CorrelationId = Guid.NewGuid(),
            CausationId = Guid.NewGuid()
        };

        var sut = new ScoringEngine(
            new NullLogger<ScoringEngine>(),
            _eventBusStub,
            _playthroughRepo,
            new ThrowingScoreCalculator(),
            _tracker,
            new EvaluationOptions()
        );

        var result = await sut.OnEventAsync(evt);
        Assert.False(result.Acknowledged);

        var updatedPt = await _playthroughRepo.GetByIdAsync(ptId);
        Assert.False(updatedPt!.EvaluationCompleted);
        Assert.Empty(_eventBusStub.PublishedEvents);
    }

    private class ThrowingRepository : IPlaythroughRepository
    {
        public Task<Playthrough?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Playthrough?>(new Playthrough(id, "m1"));
        public Task SaveAsync(Playthrough playthrough, CancellationToken cancellationToken = default) => throw new InvalidOperationException("Persistence failed");
    }

    [Fact]
    public async Task OnEventAsync_PersistenceFailure_DoesNotPublishEvents()
    {
        var evt = new MissionCompletedEvent
        {
            SessionId = Guid.NewGuid(),
            PlaythroughId = Guid.NewGuid(),
            MissionId = "m1",
            CorrelationId = Guid.NewGuid(),
            CausationId = Guid.NewGuid()
        };

        var sut = new ScoringEngine(
            new NullLogger<ScoringEngine>(),
            _eventBusStub,
            new ThrowingRepository(),
            _scoreCalculatorStub,
            _tracker,
            new EvaluationOptions()
        );

        var result = await sut.OnEventAsync(evt);
        Assert.False(result.Acknowledged);
        Assert.Empty(_eventBusStub.PublishedEvents);
    }
}
