using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Application.Events;
using ArchitectsJourney.Application.Events.Architecture;
using ArchitectsJourney.Application.Events.Rule;
using ArchitectsJourney.Application.Events.System;
using ArchitectsJourney.Domain.Aggregates;
using ArchitectsJourney.Domain.Entities;
using ArchitectsJourney.Domain.Enums;
using ArchitectsJourney.Domain.ValueObjects;
using ArchitectsJourney.Engines.Mission;
using ArchitectsJourney.Infrastructure.EventTracker;
using ArchitectsJourney.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArchitectsJourney.Tests.Engines;

public class MissionEngineTests
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

    private class StubMissionLoader : IMissionLoader
    {
        public Mission? MissionToReturn { get; set; }
        public int LoadCallCount { get; private set; }

        public Task<Mission> LoadMissionAsync(string missionId, CancellationToken cancellationToken = default)
        {
            LoadCallCount++;
            return Task.FromResult(MissionToReturn ?? new Mission(missionId) 
            { 
                Version = "1", Title = "T", Description = "D", 
                InitialMetrics = new Dictionary<string, int>(), 
                InitialNodes = [], InitialEdges = [], DecisionPoints = [], Rules = [], Objectives = [] 
            });
        }
    }

    private readonly MissionEngine _sut;
    private readonly StubEventBus _eventBusStub;
    private readonly InMemoryPlaythroughRepository _playthroughRepo;
    private readonly StubMissionLoader _missionLoaderStub;
    private readonly InMemoryEventIdempotencyTracker _tracker;

    public MissionEngineTests()
    {
        _eventBusStub = new StubEventBus();
        _playthroughRepo = new InMemoryPlaythroughRepository();
        _missionLoaderStub = new StubMissionLoader();
        _tracker = new InMemoryEventIdempotencyTracker();

        _sut = new MissionEngine(
            new NullLogger<MissionEngine>(),
            _eventBusStub,
            _playthroughRepo,
            _missionLoaderStub,
            _tracker
        );
    }

    [Fact]
    public async Task Evaluate_WhenMetricObjectiveMet_CompletesObjectiveAndFiresEvents()
    {
        // Arrange
        var ptId = Guid.NewGuid();
        var missionId = "M1";
        
        var playthrough = new Playthrough(ptId, missionId);
        playthrough.InitializeMetrics(new Dictionary<MetricType, int> { { MetricType.Cost, 40 } });
        await _playthroughRepo.SaveAsync(playthrough);

        var mission = new Mission(missionId)
        {
            Version = "1", Title = "T", Description = "D",
            InitialMetrics = new Dictionary<string, int>(),
            InitialNodes = [], InitialEdges = [], DecisionPoints = [], Rules = [],
            Objectives = [
                new MissionObjectiveDefinition
                {
                    Id = "Obj1",
                    Description = "Keep cost <= 50",
                    Conditions = [new ObjectiveCondition { Type = "Metric", Target = "Cost", Operator = "<=", Value = "50" }]
                }
            ]
        };

        _missionLoaderStub.MissionToReturn = mission;

        var evt = new DecisionProcessedEvent
        {
            SessionId = Guid.NewGuid(), PlaythroughId = ptId, MissionId = missionId,
            CorrelationId = Guid.NewGuid(), CausationId = Guid.NewGuid(),
            DecisionPointId = "DP1", OptionId = "O1",
            AppliedRuleIds = [], PhaseCompleted = false
        };

        // Act
        var result = await _sut.OnEventAsync(evt);

        // Assert
        Assert.True(result.Acknowledged);
        var pt = await _playthroughRepo.GetByIdAsync(ptId);
        Assert.Equal(ObjectiveState.Completed, pt!.Objectives[0].State);

        Assert.Contains(_eventBusStub.PublishedEvents, e => e is MissionObjectiveCompletedEvent oc && oc.ObjectiveId == "Obj1");
        Assert.Contains(_eventBusStub.PublishedEvents, e => e is MissionCompletedEvent);
    }

    [Fact]
    public async Task Event_DuplicateEvent_IsIgnored()
    {
        // Arrange
        var ptId = Guid.NewGuid();
        var missionId = "M1";
        var playthrough = new Playthrough(ptId, missionId);
        await _playthroughRepo.SaveAsync(playthrough);
        
        var mission = new Mission(missionId) { Version = "1", Title = "T", Description = "D", InitialMetrics = new Dictionary<string, int>(), InitialNodes = [], InitialEdges = [], DecisionPoints = [], Rules = [], Objectives = [] };
        _missionLoaderStub.MissionToReturn = mission;

        var evt = new DecisionProcessedEvent
        {
            EventId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(), PlaythroughId = ptId, MissionId = missionId,
            CorrelationId = Guid.NewGuid(), CausationId = Guid.NewGuid(),
            DecisionPointId = "DP1", OptionId = "O1",
            AppliedRuleIds = [], PhaseCompleted = false
        };

        // Act
        var r1 = await _sut.OnEventAsync(evt);
        var r2 = await _sut.OnEventAsync(evt);

        // Assert
        Assert.True(r1.Acknowledged);
        Assert.True(r2.Acknowledged);
        
        Assert.Equal(1, _missionLoaderStub.LoadCallCount); // The second event shouldn't even load the mission
    }
}
