using ArchitectsJourney.Application.Common;
using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Application.Events;
using ArchitectsJourney.Application.Events.Player;
using ArchitectsJourney.Domain.Aggregates;
using ArchitectsJourney.Domain.Entities;
using ArchitectsJourney.Domain.Enums;
using ArchitectsJourney.Domain.ValueObjects;
using ArchitectsJourney.Engines.Architecture;
using ArchitectsJourney.Engines.Game;
using ArchitectsJourney.Engines.Metric;
using ArchitectsJourney.Engines.Rule;
using ArchitectsJourney.Engines.Rule.Parsing;
using ArchitectsJourney.Infrastructure.EventBus;
using ArchitectsJourney.Infrastructure.EventTracker;
using ArchitectsJourney.Infrastructure.Metrics;
using ArchitectsJourney.Infrastructure.Persistence;
using ArchitectsJourney.Infrastructure.Architecture;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ArchitectsJourney.Tests;

public sealed class PlaythroughIntegrationTests
{
    [Fact]
    public async Task CompleteMissionPlaythrough_ShouldExecuteSuccessfully()
    {
        // 1. Build Dependency Injection Container
        var services = new ServiceCollection();

        services.AddSingleton<IMissionRepository, InMemoryMissionRepository>();
        services.AddSingleton<IPlaythroughRepository, InMemoryPlaythroughRepository>();
        services.AddSingleton<IAuditEventStore, InMemoryAuditEventStore>();
        services.AddSingleton<ISaveSystem, InMemorySaveSystem>();

        // Logging
        services.AddSingleton<ILogger<GameEngine>>(NullLogger<GameEngine>.Instance);
        services.AddSingleton<ILogger<RuleEngine>>(NullLogger<RuleEngine>.Instance);
        services.AddSingleton<ILogger<MetricEngine>>(NullLogger<MetricEngine>.Instance);
        services.AddSingleton<ILogger<ArchitectureEngine>>(NullLogger<ArchitectureEngine>.Instance);
        services.AddSingleton<ILogger<InMemoryEventBus>>(NullLogger<InMemoryEventBus>.Instance);

        // Register Subsystems with Forwarding
        services.AddSingleton<GameEngine>();
        services.AddSingleton<IGameSubsystem>(sp => sp.GetRequiredService<GameEngine>());
        services.AddSingleton<ICheckpointAware>(sp => sp.GetRequiredService<GameEngine>());

        services.AddSingleton<IConditionEvaluator, StringCompatibilityConditionEvaluator>();
        services.AddSingleton<IEffectParser, StringCompatibilityEffectParser>();

        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?> {
            {"MetricEngine:Thresholds:0", "25"},
            {"MetricEngine:Thresholds:1", "50"},
            {"MetricEngine:Thresholds:2", "75"}
        });
        services.AddSingleton<IMetricConfiguration>(new MetricConfiguration(configBuilder.Build()));
        services.AddSingleton<IEventIdempotencyTracker, InMemoryEventIdempotencyTracker>();

        services.AddSingleton<RuleEngine>();
        services.AddSingleton<IGameSubsystem>(sp => sp.GetRequiredService<RuleEngine>());

        services.AddSingleton<MetricEngine>();
        services.AddSingleton<IGameSubsystem>(sp => sp.GetRequiredService<MetricEngine>());

        services.AddSingleton(new ArchitectureValidationOptions { AllowCycles = false, AllowDanglingEdges = false, RequireStrictTechAssignments = true });
        services.AddSingleton<IArchitectureValidator, ArchitectureValidator>();
        services.AddSingleton<ArchitectureEngine>();
        services.AddSingleton<IGameSubsystem>(sp => sp.GetRequiredService<ArchitectureEngine>());

        // Event Bus
        services.AddSingleton<IEventBus, InMemoryEventBus>();

        var provider = services.BuildServiceProvider();

        // 2. Setup Event Bus Publishers and Subscribers
        var eventBus = provider.GetRequiredService<IEventBus>();
        
        eventBus.RegisterPublisher(new PublisherRegistration
        {
            PublisherId = "GAME_ENGINE",
            AuthorizedEventTypes = [
                EventTypes.System.SessionStarted,
                EventTypes.System.MissionLoaded,
                EventTypes.System.MissionPhaseChanged,
                EventTypes.System.RollbackRequired,
                EventTypes.Rule.DecisionProcessed,
                EventTypes.System.CheckpointCreated,
                EventTypes.Narrative.FeedbackDelivered,
                EventTypes.Narrative.FeedbackQueued
            ]
        });
        
        eventBus.RegisterSubscriber(new SubscriptionRegistration
        {
            SubscriberId = "GAME_ENGINE",
            Type = SubscriptionType.Type,
            TargetEventType = EventTypes.Player.DecisionSubmitted,
            RequiresAcknowledgement = true,
            DeliveryOrder = 1
        });

        eventBus.RegisterPublisher(new PublisherRegistration
        {
            PublisherId = "UI_LAYER",
            AuthorizedEventTypes = [
                EventTypes.Player.DecisionSubmitted,
                EventTypes.Player.QuestionAsked
            ]
        });

        eventBus.RegisterPublisher(new PublisherRegistration
        {
            PublisherId = "RULE_ENGINE",
            AuthorizedEventTypes = [
                EventTypes.Rule.DecisionProcessed,
                EventTypes.Rule.QuestionProcessed,
                EventTypes.Rule.DerivedRuleTriggered,
                EventTypes.Rule.RuleExecutionAuditCreated,
                EventTypes.Rule.BusinessEventProcessed,
                EventTypes.Metric.DeltaApplied
            ]
        });

        eventBus.RegisterPublisher(new PublisherRegistration
        {
            PublisherId = "METRIC_ENGINE",
            AuthorizedEventTypes = [
                EventTypes.Metric.DeltaApplied,
                EventTypes.Metric.ThresholdCrossed,
                EventTypes.Metric.BoundsEnforced
            ]
        });
        
        eventBus.RegisterSubscriber(new SubscriptionRegistration
        {
            SubscriberId = "METRIC_ENGINE",
            Type = SubscriptionType.Type,
            TargetEventType = EventTypes.Rule.DecisionProcessed,
            RequiresAcknowledgement = true,
            DeliveryOrder = 2
        });

        eventBus.RegisterSubscriber(new SubscriptionRegistration
        {
            SubscriberId = "METRIC_ENGINE",
            Type = SubscriptionType.Type,
            TargetEventType = EventTypes.Metric.DeltaApplied,
            RequiresAcknowledgement = true,
            DeliveryOrder = 2
        });

        eventBus.RegisterPublisher(new PublisherRegistration
        {
            PublisherId = "ARCHITECTURE_ENGINE",
            AuthorizedEventTypes = [
                EventTypes.Architecture.Initialized,
                EventTypes.Architecture.NodeAdded,
                EventTypes.Architecture.NodeRemoved,
                EventTypes.Architecture.EdgeAdded,
                EventTypes.Architecture.EdgeRemoved
            ]
        });

        eventBus.RegisterSubscriber(new SubscriptionRegistration
        {
            SubscriberId = "ARCHITECTURE_ENGINE",
            Type = SubscriptionType.Type,
            TargetEventType = EventTypes.Architecture.Changed,
            RequiresAcknowledgement = true,
            DeliveryOrder = 2
        });

        // 3. Seed authored mission content
        var missionRepo = provider.GetRequiredService<IMissionRepository>();
        var missionId = "m1";
        
        var seedMission = new Mission(missionId)
        {
            Title = "Cloud Migration Test",
            Description = "Seed testing mission",
            Version = "1.0",
            InitialMetrics = new Dictionary<string, int>
            {
                { "Performance", 80 },
                { "Scalability", 50 },
                { "Reliability", 60 },
                { "Maintainability", 40 },
                { "Complexity", 30 },
                { "Cost", 90 },
                { "Security", 75 }
            },
            InitialNodes = [],
            InitialEdges = [],
            Rules = [],
            DecisionPoints = [
                new DecisionPoint("dp_monolith")
                {
                    Title = "Deconstruct Monolith",
                    Dialogue = "Should we break it down?",
                    Phase = MissionPhase.ClientMeeting,
                    Options = [
                        new DecisionOption("opt_microservices")
                        {
                            Label = "Go Microservices",
                            Description = "Refactor into separate services",
                            Condition = null,
                            GraphMutations = [],
                            MetricImpacts = [
                                new MetricChangeEffect
                                {
                                    Metric = MetricType.Scalability,
                                    Label = DeltaLabel.MajorImprovement,
                                    Value = 25
                                },
                                new MetricChangeEffect
                                {
                                    Metric = MetricType.Reliability,
                                    Label = DeltaLabel.MinorDegradation,
                                    Value = -10
                                }
                            ]
                        }
                    ]
                }
            ]
        };

        await missionRepo.SaveAsync(seedMission);

        // 4. Initialize session context
        var context = new SessionContext
        {
            SessionId = Guid.NewGuid(),
            PlaythroughId = Guid.NewGuid(),
            MissionId = missionId,
            PlayerId = Guid.NewGuid()
        };

        // Initialize all subsystems
        var subsystems = provider.GetServices<IGameSubsystem>();
        foreach (var sub in subsystems)
        {
            var initRes = await sub.InitializeAsync(context);
            Assert.True(initRes.Success);
        }

        // 5. Submit Player Decision (Phase 01-02 Loop trigger)
        var decisionEvent = new PlayerDecisionSubmittedEvent
        {
            SessionId = context.SessionId,
            PlaythroughId = context.PlaythroughId,
            MissionId = context.MissionId,
            CorrelationId = Guid.NewGuid(),
            CausationId = null,
            DecisionPointId = "dp_monolith",
            SelectedOptionId = "opt_microservices"
        };

        var publishResult = await eventBus.PublishAsync(decisionEvent);
        Assert.True(publishResult.Accepted);

        // Wait for asynchronous event bus to drain (allow cascaded events)
        // Polling to avoid race conditions
        var playthroughRepo = provider.GetRequiredService<IPlaythroughRepository>();
        Playthrough? playthrough = null;
        for (int i = 0; i < 20; i++)
        {
            playthrough = await playthroughRepo.GetByIdAsync(context.PlaythroughId);
            if (playthrough != null)
            {
                playthrough.Metrics.TryGetValue(MetricType.Scalability, out int currentScalability);
                playthrough.Metrics.TryGetValue(MetricType.Reliability, out int currentReliability);
                if (currentScalability == 75 && currentReliability == 50)
                    break;
            }
            await Task.Delay(100);
        }
        
        Assert.NotNull(playthrough);
        
        // Assert that the decision was resolved and registered in playthrough aggregate
        Assert.Contains("dp_monolith", playthrough.ResolvedDecisions);

        // Assert that metric delta changes were clamped and saved correctly:
        // Scalability: 50 (base) + 25 = 75
        // Reliability: 60 (base) - 10 = 50
        playthrough.Metrics.TryGetValue(MetricType.Scalability, out int scalability);
        playthrough.Metrics.TryGetValue(MetricType.Reliability, out int reliability);
        
        Assert.Equal(75, scalability);
        Assert.Equal(50, reliability);

        // Assert that a checkpoint was created
        var saveSystem = provider.GetRequiredService<ISaveSystem>();
        var checkpoint = await saveSystem.LoadLastCheckpointAsync(context.SessionId);
        Assert.NotNull(checkpoint);
        Assert.Single(checkpoint); // Only GameEngine registered as ICheckpointAware since ArchitectureEngine is stateless
    }
}
