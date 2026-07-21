using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Application.Events.System;
using ArchitectsJourney.Application.Models;
using ArchitectsJourney.Domain.Aggregates;
using ArchitectsJourney.Engines.Achievements;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArchitectsJourney.Tests.Achievements;

public class AchievementEngineTests
{
    private readonly StubEventBus _eventBus = new();
    private readonly StubPlaythroughRepository _repo = new();
    private readonly StubIdempotencyTracker _tracker = new();
    private readonly StubAchievementEvaluator _evaluator = new();

    private AchievementEngine CreateSut()
    {
        return new AchievementEngine(
            NullLogger<AchievementEngine>.Instance,
            _eventBus,
            _repo,
            _evaluator,
            _tracker,
            new AchievementOptions()
        );
    }

    [Fact]
    public async Task OnEventAsync_NoChanges_AckAndNoEvents()
    {
        var sut = CreateSut();
        var pt = new Playthrough(Guid.NewGuid(), "M1");
        await _repo.SaveAsync(pt);

        var evt = new MissionEvaluationCompletedEvent
        {
            SessionId = Guid.NewGuid(),
            PlaythroughId = pt.Id,
            MissionId = "M1",
            MissionResult = "Success"
        };

        var result = await sut.OnEventAsync(evt);
        Assert.True(result.Acknowledged);
        Assert.Empty(_eventBus.PublishedEvents);
    }

    [Fact]
    public async Task OnEventAsync_EvaluatorYieldsChanges_SavesAndPublishes()
    {
        var sut = CreateSut();
        var pt = new Playthrough(Guid.NewGuid(), "M1");
        await _repo.SaveAsync(pt);

        _evaluator.ResultToReturn = new AchievementEvaluationResult
        {
            UnlockedAchievements = ["ACH1"],
            AwardedExperience = 100,
            NewLevel = 2,
            Notifications = []
        };

        var evt = new MissionEvaluationCompletedEvent
        {
            SessionId = Guid.NewGuid(),
            PlaythroughId = pt.Id,
            MissionId = "M1",
            MissionResult = "Success"
        };

        var result = await sut.OnEventAsync(evt);
        Assert.True(result.Acknowledged);

        var updated = await _repo.GetByIdAsync(pt.Id);
        Assert.Contains("ACH1", updated!.UnlockedAchievements);
        Assert.Equal(100, updated.ExperiencePoints);
        Assert.Equal(2, updated.PlayerLevel);

        Assert.Equal(3, _eventBus.PublishedEvents.Count);
        Assert.IsType<AchievementUnlockedEvent>(_eventBus.PublishedEvents[0]);
        Assert.IsType<ExperienceAwardedEvent>(_eventBus.PublishedEvents[1]);
        Assert.IsType<PlayerLevelChangedEvent>(_eventBus.PublishedEvents[2]);
    }

    private class StubEventBus : IEventBus
    {
        public List<ArchitectsJourney.Application.Events.DomainEvent> PublishedEvents { get; } = [];
        public Task<PublicationResult> PublishAsync(ArchitectsJourney.Application.Events.DomainEvent @event, CancellationToken ct = default)
        {
            PublishedEvents.Add(@event);
            return Task.FromResult(PublicationResult.Buffered(@event.EventId));
        }

        public void RegisterPublisher(PublisherRegistration registration) { }
        public void RegisterSubscriber(SubscriptionRegistration registration) { }
#pragma warning disable CA1822 // Mark members as static
        public Task SubscribeAsync<TEvent>(string subscriberId, SubscriptionType type, Func<TEvent, Task> handler) where TEvent : ArchitectsJourney.Application.Events.DomainEvent => Task.CompletedTask;
        public Task UnsubscribeAsync(string subscriberId, string eventType) => Task.CompletedTask;
#pragma warning restore CA1822 // Mark members as static
    }

    private class StubPlaythroughRepository : IPlaythroughRepository
    {
        private readonly Dictionary<Guid, Playthrough> _pts = [];
        public Task<Playthrough?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(_pts.GetValueOrDefault(id));
        public Task SaveAsync(Playthrough playthrough, CancellationToken ct = default)
        {
            _pts[playthrough.Id] = playthrough;
            return Task.CompletedTask;
        }
    }

    private class StubIdempotencyTracker : IEventIdempotencyTracker
    {
#pragma warning disable CA1822 // Mark members as static
        public Task<bool> TryMarkProcessedAsync(Guid contextId, Guid eventId, CancellationToken ct = default) => Task.FromResult(true);
        public Task ClearContextAsync(Guid contextId, CancellationToken ct = default) => Task.CompletedTask;
#pragma warning restore CA1822 // Mark members as static
    }

    private class StubAchievementEvaluator : IAchievementEvaluator
    {
        public AchievementEvaluationResult ResultToReturn { get; set; } = new AchievementEvaluationResult
        {
            UnlockedAchievements = [],
            AwardedExperience = 0,
            NewLevel = 1,
            Notifications = []
        };

        public AchievementEvaluationResult Evaluate(Playthrough pt, AchievementOptions options) => ResultToReturn;
    }
}
