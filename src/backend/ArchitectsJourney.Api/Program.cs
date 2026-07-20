using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Engines.Architecture;
using ArchitectsJourney.Engines.Game;
using ArchitectsJourney.Engines.Metric;
using ArchitectsJourney.Engines.Rule;
using ArchitectsJourney.Engines.Rule.Parsing;
using ArchitectsJourney.Infrastructure.EventBus;
using ArchitectsJourney.Infrastructure.MissionLoading;
using ArchitectsJourney.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 1. Register Persistence Mocks
builder.Services.AddSingleton<IMissionRepository, InMemoryMissionRepository>();
builder.Services.AddSingleton<IPlaythroughRepository, InMemoryPlaythroughRepository>();
builder.Services.AddSingleton<IAuditEventStore, InMemoryAuditEventStore>();
builder.Services.AddSingleton<ISaveSystem, InMemorySaveSystem>();

// Mission Loading Pipeline
builder.Services.AddSingleton<ITechnologyCatalog, InMemoryTechnologyCatalog>();
builder.Services.AddSingleton<IMissionCache, InMemoryMissionCache>();
builder.Services.AddTransient<IMissionDiscovery, MissionDiscovery>();
builder.Services.AddTransient<IMissionReader, MissionReader>();
builder.Services.AddTransient<IMissionSchemaValidator, MissionSchemaValidator>();
builder.Services.AddTransient<IMissionReferenceValidator, MissionReferenceValidator>();
builder.Services.AddTransient<IMissionBuilder, MissionBuilder>();
builder.Services.AddTransient<IMissionLoader, MissionLoader>();

// 2. Register Subsystems with Interface Forwarding to avoid duplicate instances (Doc 09, Section 12)
builder.Services.AddSingleton<GameEngine>();
builder.Services.AddSingleton<IGameSubsystem>(sp => sp.GetRequiredService<GameEngine>());
builder.Services.AddSingleton<ICheckpointAware>(sp => sp.GetRequiredService<GameEngine>());

builder.Services.AddSingleton<IConditionEvaluator, StringCompatibilityConditionEvaluator>();
builder.Services.AddSingleton<IEffectParser, StringCompatibilityEffectParser>();
builder.Services.AddSingleton<RuleEngine>();
builder.Services.AddSingleton<IGameSubsystem>(sp => sp.GetRequiredService<RuleEngine>());

builder.Services.AddSingleton<IEventIdempotencyTracker, ArchitectsJourney.Infrastructure.EventTracker.InMemoryEventIdempotencyTracker>();
builder.Services.AddSingleton<IMetricConfiguration, ArchitectsJourney.Infrastructure.Metrics.MetricConfiguration>();
builder.Services.AddSingleton<MetricEngine>();
builder.Services.AddSingleton<IGameSubsystem>(sp => sp.GetRequiredService<MetricEngine>());

builder.Services.AddSingleton(new ArchitectureValidationOptions { AllowCycles = false, AllowDanglingEdges = false, RequireStrictTechAssignments = true });
builder.Services.AddSingleton<IArchitectureValidator, ArchitectsJourney.Infrastructure.Architecture.ArchitectureValidator>();
builder.Services.AddSingleton<ArchitectureEngine>();
builder.Services.AddSingleton<IGameSubsystem>(sp => sp.GetRequiredService<ArchitectureEngine>());

// 3. Register Event Bus
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();

// Health Check Endpoint
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

// Register Authorized Publishers and Subscriptions
var eventBus = app.Services.GetRequiredService<IEventBus>();

// Register GameEngine as publisher and subscriber
eventBus.RegisterPublisher(new PublisherRegistration
{
    PublisherId = "GAME_ENGINE",
    AuthorizedEventTypes = [
        ArchitectsJourney.Application.Events.EventTypes.System.SessionStarted,
        ArchitectsJourney.Application.Events.EventTypes.System.MissionLoaded,
        ArchitectsJourney.Application.Events.EventTypes.System.MissionPhaseChanged,
        ArchitectsJourney.Application.Events.EventTypes.System.RollbackRequired,
        ArchitectsJourney.Application.Events.EventTypes.Rule.DecisionProcessed
    ]
});
eventBus.RegisterSubscriber(new SubscriptionRegistration
{
    SubscriberId = "GAME_ENGINE",
    Type = SubscriptionType.Type,
    TargetEventType = ArchitectsJourney.Application.Events.EventTypes.Player.DecisionSubmitted,
    RequiresAcknowledgement = true,
    DeliveryOrder = 1
});

// Register UI Layer as publisher
eventBus.RegisterPublisher(new PublisherRegistration
{
    PublisherId = "UI_LAYER",
    AuthorizedEventTypes = [
        ArchitectsJourney.Application.Events.EventTypes.Player.DecisionSubmitted,
        ArchitectsJourney.Application.Events.EventTypes.Player.QuestionAsked
    ]
});

// Register RuleEngine as publisher and subscriber
eventBus.RegisterPublisher(new PublisherRegistration
{
    PublisherId = "RULE_ENGINE",
    AuthorizedEventTypes = [
        ArchitectsJourney.Application.Events.EventTypes.Rule.DecisionProcessed,
        ArchitectsJourney.Application.Events.EventTypes.Rule.QuestionProcessed,
        ArchitectsJourney.Application.Events.EventTypes.Rule.DerivedRuleTriggered
    ]
});
eventBus.RegisterSubscriber(new SubscriptionRegistration
{
    SubscriberId = "RULE_ENGINE",
    Type = SubscriptionType.Type,
    TargetEventType = ArchitectsJourney.Application.Events.EventTypes.Rule.DecisionProcessed,
    RequiresAcknowledgement = true,
    DeliveryOrder = 1
});

// Register MetricEngine as publisher and subscriber
eventBus.RegisterPublisher(new PublisherRegistration
{
    PublisherId = "METRIC_ENGINE",
    AuthorizedEventTypes = [
        ArchitectsJourney.Application.Events.EventTypes.Metric.DeltaApplied,
        ArchitectsJourney.Application.Events.EventTypes.Metric.ThresholdCrossed,
        ArchitectsJourney.Application.Events.EventTypes.Metric.BoundsEnforced
    ]
});
eventBus.RegisterSubscriber(new SubscriptionRegistration
{
    SubscriberId = "METRIC_ENGINE",
    Type = SubscriptionType.Type,
    TargetEventType = ArchitectsJourney.Application.Events.EventTypes.Rule.DecisionProcessed,
    RequiresAcknowledgement = true,
    DeliveryOrder = 2
});
eventBus.RegisterSubscriber(new SubscriptionRegistration
{
    SubscriberId = "METRIC_ENGINE",
    Type = SubscriptionType.Type,
    TargetEventType = ArchitectsJourney.Application.Events.EventTypes.Metric.DeltaApplied,
    RequiresAcknowledgement = true,
    DeliveryOrder = 2
});

// Register ArchitectureEngine as publisher and subscriber
eventBus.RegisterPublisher(new PublisherRegistration
{
    PublisherId = "ARCHITECTURE_ENGINE",
    AuthorizedEventTypes = [
        ArchitectsJourney.Application.Events.EventTypes.Architecture.Initialized,
        ArchitectsJourney.Application.Events.EventTypes.Architecture.NodeAdded,
        ArchitectsJourney.Application.Events.EventTypes.Architecture.NodeRemoved,
        ArchitectsJourney.Application.Events.EventTypes.Architecture.EdgeAdded,
        ArchitectsJourney.Application.Events.EventTypes.Architecture.EdgeRemoved
    ]
});
eventBus.RegisterSubscriber(new SubscriptionRegistration
{
    SubscriberId = "ARCHITECTURE_ENGINE",
    Type = SubscriptionType.Type,
    TargetEventType = ArchitectsJourney.Application.Events.EventTypes.Architecture.Changed,
    RequiresAcknowledgement = true,
    DeliveryOrder = 2
});

app.Run();
