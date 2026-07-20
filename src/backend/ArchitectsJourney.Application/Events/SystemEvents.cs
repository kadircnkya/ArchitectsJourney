using ArchitectsJourney.Application.Common;
using ArchitectsJourney.Application.Events;

namespace ArchitectsJourney.Application.Events.System;

public sealed record MissionLoadedEvent : DomainEvent
{
    public override string EventType => EventTypes.System.MissionLoaded;
    public override EventCategory EventCategory => EventCategory.System;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.System;
    public override string ProducerId => "GAME_ENGINE";

    public required int RuleCount { get; init; }
    public required int DecisionPointCount { get; init; }
}

public sealed record MissionPhaseChangedEvent : DomainEvent
{
    public override string EventType => EventTypes.System.MissionPhaseChanged;
    public override EventCategory EventCategory => EventCategory.System;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.System;
    public override string ProducerId => "GAME_ENGINE";

    public required string PreviousPhase { get; init; }
    public required string NewPhase { get; init; }
}

public sealed record RollbackRequiredEvent : DomainEvent
{
    public override string EventType => EventTypes.System.RollbackRequired;
    public override EventCategory EventCategory => EventCategory.System;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Critical;
    public override string ProducerId => "GAME_ENGINE";

    public required string FailedSubsystem { get; init; }
    public required string FailureReason { get; init; }
    public required Guid LastCheckpointId { get; init; }
}

public sealed record CheckpointCreatedEvent : DomainEvent
{
    public override string EventType => EventTypes.System.CheckpointCreated;
    public override EventCategory EventCategory => EventCategory.System;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.System;
    public override string ProducerId => "GAME_ENGINE";

    public required Guid CheckpointId { get; init; }
}
