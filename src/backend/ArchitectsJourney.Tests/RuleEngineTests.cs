using ArchitectsJourney.Application.Common;
using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Application.Events;
using ArchitectsJourney.Application.Events.Architecture;
using ArchitectsJourney.Application.Events.Metric;
using ArchitectsJourney.Application.Events.Rule;
using ArchitectsJourney.Domain.Aggregates;
using ArchitectsJourney.Domain.Entities;
using ArchitectsJourney.Domain.Enums;
using ArchitectsJourney.Engines.Rule;
using ArchitectsJourney.Engines.Rule.Parsing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ArchitectsJourney.Tests;

public class RuleEngineTests
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

    private class StubMissionRepository : IMissionRepository
    {
        public Mission? CurrentMission { get; set; }
        public Task<Mission?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(CurrentMission);
        public Task SaveAsync(Mission mission, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<Mission>> ListAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Mission>>(new[] { CurrentMission }.Where(m => m != null).Cast<Mission>().ToList());
    }

    private class StubPlaythroughRepository : IPlaythroughRepository
    {
        public Playthrough? CurrentPlaythrough { get; set; }
        public Task<Playthrough?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(CurrentPlaythrough);
        public Task SaveAsync(Playthrough playthrough, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [Fact]
    public async Task OnEventAsync_EvaluatesCondition_And_PublishesEffects()
    {
        // Arrange
        var eventBus = new StubEventBus();
        var missionRepo = new StubMissionRepository();
        var playthroughRepo = new StubPlaythroughRepository();
        var conditionEval = new StringCompatibilityConditionEvaluator();
        var effectParser = new StringCompatibilityEffectParser();
        var logger = NullLogger<RuleEngine>.Instance;

        var engine = new RuleEngine(logger, eventBus, missionRepo, playthroughRepo, conditionEval, effectParser);

        var mission = new Mission("m1")
        {
            Title = "Title",
            Description = "Desc",
            Version = "1.0",
            InitialMetrics = new Dictionary<string, int>(),
            InitialNodes = [],
            InitialEdges = [],
            DecisionPoints = [],
            Rules = [
                new MissionRuleDefinition
                {
                    RuleId = "rule1",
                    Trigger = "DECISION_RESOLVED",
                    Condition = "metric:Scalability > 50 AND tech:Docker",
                    Effects = ["MetricChangeEffect:Performance:10", "ArchitectureEffect:ADD:Node1"]
                }
            ]
        };
        missionRepo.CurrentMission = mission;

        var playthrough = new Playthrough(Guid.NewGuid(), "m1");
        playthrough.InitializeMetrics(new Dictionary<MetricType, int> { { MetricType.Scalability, 60 } });
        playthrough.UnlockTechnology("Docker");
        playthroughRepo.CurrentPlaythrough = playthrough;

        var context = new SessionContext { SessionId = Guid.NewGuid(), MissionId = "m1", PlaythroughId = playthrough.Id, PlayerId = Guid.NewGuid() };
        await engine.InitializeAsync(context);

        var decisionEvent = new DecisionProcessedEvent
        {
            SessionId = context.SessionId,
            PlaythroughId = playthrough.Id,
            MissionId = "m1",
            CorrelationId = Guid.NewGuid(),
            CausationId = null,
            DecisionPointId = "dp1",
            OptionId = "opt1",
            AppliedRuleIds = [],
            PhaseCompleted = false
        };

        // Act
        var result = await engine.OnEventAsync(decisionEvent);

        // Assert
        Assert.True(result.Acknowledged);
        Assert.Contains(eventBus.PublishedEvents, e => e is MetricDeltaAppliedEvent m && m.Metric == "Performance" && m.Value == 10);
        Assert.Contains(eventBus.PublishedEvents, e => e is ArchitectureChangedEvent a && a.MutationType == "ADD" && a.TargetId == "Node1");
        Assert.Contains(eventBus.PublishedEvents, e => e is RuleExecutionAuditCreatedEvent a && a.RuleId == "rule1");
    }

    [Fact]
    public async Task OnEventAsync_FalseCondition_DoesNotPublish()
    {
        var eventBus = new StubEventBus();
        var missionRepo = new StubMissionRepository();
        var playthroughRepo = new StubPlaythroughRepository();
        var conditionEval = new StringCompatibilityConditionEvaluator();
        var effectParser = new StringCompatibilityEffectParser();
        var logger = NullLogger<RuleEngine>.Instance;

        var engine = new RuleEngine(logger, eventBus, missionRepo, playthroughRepo, conditionEval, effectParser);

        var mission = new Mission("m1")
        {
            Title = "Title",
            Description = "Desc",
            Version = "1.0",
            InitialMetrics = new Dictionary<string, int>(),
            InitialNodes = [],
            InitialEdges = [],
            DecisionPoints = [],
            Rules = [
                new MissionRuleDefinition
                {
                    RuleId = "rule1",
                    Trigger = "DECISION_RESOLVED",
                    Condition = "metric:Scalability > 80",
                    Effects = ["MetricChangeEffect:Performance:10"]
                }
            ]
        };
        missionRepo.CurrentMission = mission;

        var playthrough = new Playthrough(Guid.NewGuid(), "m1");
        playthrough.InitializeMetrics(new Dictionary<MetricType, int> { { MetricType.Scalability, 60 } });
        playthroughRepo.CurrentPlaythrough = playthrough;

        var context = new SessionContext { SessionId = Guid.NewGuid(), MissionId = "m1", PlaythroughId = playthrough.Id, PlayerId = Guid.NewGuid() };
        await engine.InitializeAsync(context);

        var decisionEvent = new DecisionProcessedEvent
        {
            SessionId = context.SessionId,
            PlaythroughId = playthrough.Id,
            MissionId = "m1",
            CorrelationId = Guid.NewGuid(),
            CausationId = null,
            DecisionPointId = "dp1",
            OptionId = "opt1",
            AppliedRuleIds = [],
            PhaseCompleted = false
        };

        await engine.OnEventAsync(decisionEvent);

        Assert.Empty(eventBus.PublishedEvents);
    }

    [Fact]
    public async Task OnEventAsync_PreventsDuplicateExecution()
    {
        var eventBus = new StubEventBus();
        var missionRepo = new StubMissionRepository();
        var playthroughRepo = new StubPlaythroughRepository();
        var conditionEval = new StringCompatibilityConditionEvaluator();
        var effectParser = new StringCompatibilityEffectParser();
        var logger = NullLogger<RuleEngine>.Instance;

        var engine = new RuleEngine(logger, eventBus, missionRepo, playthroughRepo, conditionEval, effectParser);

        var mission = new Mission("m1")
        {
            Title = "Title",
            Description = "Desc",
            Version = "1.0",
            InitialMetrics = new Dictionary<string, int>(),
            InitialNodes = [],
            InitialEdges = [],
            DecisionPoints = [],
            Rules = [
                new MissionRuleDefinition
                {
                    RuleId = "rule1",
                    Trigger = "DECISION_RESOLVED",
                    Condition = "",
                    Effects = ["MetricChangeEffect:Performance:10"]
                }
            ]
        };
        missionRepo.CurrentMission = mission;

        var playthrough = new Playthrough(Guid.NewGuid(), "m1");
        playthroughRepo.CurrentPlaythrough = playthrough;

        var context = new SessionContext { SessionId = Guid.NewGuid(), MissionId = "m1", PlaythroughId = playthrough.Id, PlayerId = Guid.NewGuid() };
        await engine.InitializeAsync(context);

        var correlationId = Guid.NewGuid();

        var decisionEvent = new DecisionProcessedEvent
        {
            SessionId = context.SessionId,
            PlaythroughId = playthrough.Id,
            MissionId = "m1",
            CorrelationId = correlationId,
            CausationId = null,
            DecisionPointId = "dp1",
            OptionId = "opt1",
            AppliedRuleIds = [],
            PhaseCompleted = false
        };

        // Act - First trigger
        await engine.OnEventAsync(decisionEvent);
        int publishedCount = eventBus.PublishedEvents.Count;

        // Act - Second trigger with same correlation
        await engine.OnEventAsync(decisionEvent);

        // Assert - no new events published for the same rule in the same correlation context
        Assert.Equal(publishedCount, eventBus.PublishedEvents.Count);
    }

    [Fact]
    public async Task OnEventAsync_AdditiveConflictResolution()
    {
        var eventBus = new StubEventBus();
        var missionRepo = new StubMissionRepository();
        var playthroughRepo = new StubPlaythroughRepository();
        var conditionEval = new StringCompatibilityConditionEvaluator();
        var effectParser = new StringCompatibilityEffectParser();
        var logger = NullLogger<RuleEngine>.Instance;

        var engine = new RuleEngine(logger, eventBus, missionRepo, playthroughRepo, conditionEval, effectParser);

        var mission = new Mission("m1")
        {
            Title = "Title",
            Description = "Desc",
            Version = "1.0",
            InitialMetrics = new Dictionary<string, int>(),
            InitialNodes = [],
            InitialEdges = [],
            DecisionPoints = [],
            Rules = [
                new MissionRuleDefinition
                {
                    RuleId = "rule1",
                    Trigger = "DECISION_RESOLVED",
                    Condition = "",
                    Effects = ["MetricChangeEffect:Performance:10", "MetricChangeEffect:Performance:25"]
                }
            ]
        };
        missionRepo.CurrentMission = mission;

        var playthrough = new Playthrough(Guid.NewGuid(), "m1");
        playthroughRepo.CurrentPlaythrough = playthrough;

        var context = new SessionContext { SessionId = Guid.NewGuid(), MissionId = "m1", PlaythroughId = playthrough.Id, PlayerId = Guid.NewGuid() };
        await engine.InitializeAsync(context);

        var decisionEvent = new DecisionProcessedEvent
        {
            SessionId = context.SessionId,
            PlaythroughId = playthrough.Id,
            MissionId = "m1",
            CorrelationId = Guid.NewGuid(),
            CausationId = null,
            DecisionPointId = "dp1",
            OptionId = "opt1",
            AppliedRuleIds = [],
            PhaseCompleted = false
        };

        await engine.OnEventAsync(decisionEvent);

        var metricEvent = Assert.Single(eventBus.PublishedEvents.OfType<MetricDeltaAppliedEvent>());
        Assert.Equal("Performance", metricEvent.Metric);
        Assert.Equal(35, metricEvent.Value);
    }

    [Fact]
    public async Task OnEventAsync_HandlesInvalidConditionAndEffect()
    {
        var eventBus = new StubEventBus();
        var missionRepo = new StubMissionRepository();
        var playthroughRepo = new StubPlaythroughRepository();
        var conditionEval = new StringCompatibilityConditionEvaluator();
        var effectParser = new StringCompatibilityEffectParser();
        var logger = NullLogger<RuleEngine>.Instance;

        var engine = new RuleEngine(logger, eventBus, missionRepo, playthroughRepo, conditionEval, effectParser);

        var mission = new Mission("m1")
        {
            Title = "Title",
            Description = "Desc",
            Version = "1.0",
            InitialMetrics = new Dictionary<string, int>(),
            InitialNodes = [],
            InitialEdges = [],
            DecisionPoints = [],
            Rules = [
                new MissionRuleDefinition
                {
                    RuleId = "rule1",
                    Trigger = "DECISION_RESOLVED",
                    Condition = "invalid_condition_syntax",
                    Effects = ["MetricChangeEffect:Performance:10"]
                },
                new MissionRuleDefinition
                {
                    RuleId = "rule2",
                    Trigger = "DECISION_RESOLVED",
                    Condition = "",
                    Effects = ["InvalidEffectFormat", "UnknownEffect:Arg1"]
                }
            ]
        };
        missionRepo.CurrentMission = mission;

        var playthrough = new Playthrough(Guid.NewGuid(), "m1");
        playthroughRepo.CurrentPlaythrough = playthrough;

        var context = new SessionContext { SessionId = Guid.NewGuid(), MissionId = "m1", PlaythroughId = playthrough.Id, PlayerId = Guid.NewGuid() };
        await engine.InitializeAsync(context);

        var decisionEvent = new DecisionProcessedEvent
        {
            SessionId = context.SessionId,
            PlaythroughId = playthrough.Id,
            MissionId = "m1",
            CorrelationId = Guid.NewGuid(),
            CausationId = null,
            DecisionPointId = "dp1",
            OptionId = "opt1",
            AppliedRuleIds = [],
            PhaseCompleted = false
        };

        await engine.OnEventAsync(decisionEvent);

        // rule1 has invalid condition syntax -> returns false, does not execute.
        // rule2 has true condition, executes. Invalid effect is ignored. Unknown effect is ignored.
        // Rule2 still produces AuditEvent.
        var auditEvent = Assert.Single(eventBus.PublishedEvents.OfType<RuleExecutionAuditCreatedEvent>());
        Assert.Equal("rule2", auditEvent.RuleId);
        Assert.Empty(eventBus.PublishedEvents.OfType<MetricDeltaAppliedEvent>());
    }

    [Fact]
    public async Task OnEventAsync_DerivedRuleExecution()
    {
        var eventBus = new StubEventBus();
        var missionRepo = new StubMissionRepository();
        var playthroughRepo = new StubPlaythroughRepository();
        var conditionEval = new StringCompatibilityConditionEvaluator();
        var effectParser = new StringCompatibilityEffectParser();
        var logger = NullLogger<RuleEngine>.Instance;

        var engine = new RuleEngine(logger, eventBus, missionRepo, playthroughRepo, conditionEval, effectParser);

        var mission = new Mission("m1")
        {
            Title = "Title",
            Description = "Desc",
            Version = "1.0",
            InitialMetrics = new Dictionary<string, int>(),
            InitialNodes = [],
            InitialEdges = [],
            DecisionPoints = [],
            Rules = [
                new MissionRuleDefinition
                {
                    RuleId = "rule1",
                    Trigger = "DECISION_RESOLVED",
                    Condition = "",
                    Effects = ["DerivedRule:rule2"]
                }
            ]
        };
        missionRepo.CurrentMission = mission;

        var playthrough = new Playthrough(Guid.NewGuid(), "m1");
        playthroughRepo.CurrentPlaythrough = playthrough;

        var context = new SessionContext { SessionId = Guid.NewGuid(), MissionId = "m1", PlaythroughId = playthrough.Id, PlayerId = Guid.NewGuid() };
        await engine.InitializeAsync(context);

        var decisionEvent = new DecisionProcessedEvent
        {
            SessionId = context.SessionId,
            PlaythroughId = playthrough.Id,
            MissionId = "m1",
            CorrelationId = Guid.NewGuid(),
            CausationId = null,
            DecisionPointId = "dp1",
            OptionId = "opt1",
            AppliedRuleIds = [],
            PhaseCompleted = false
        };

        await engine.OnEventAsync(decisionEvent);

        var derivedEvent = Assert.Single(eventBus.PublishedEvents.OfType<DerivedRuleTriggeredEvent>());
        Assert.Equal("rule1", derivedEvent.OriginatingRuleId);
        Assert.Equal("rule2", derivedEvent.DerivedRuleId);
    }
}
