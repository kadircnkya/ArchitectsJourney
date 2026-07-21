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

public sealed record MissionObjectiveCompletedEvent : DomainEvent
{
    public override string EventType => "MISSION_OBJECTIVE_COMPLETED";
    public override EventCategory EventCategory => EventCategory.System;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "MISSION_ENGINE";

    public required string ObjectiveId { get; init; }
}

public sealed record MissionObjectiveFailedEvent : DomainEvent
{
    public override string EventType => "MISSION_OBJECTIVE_FAILED";
    public override EventCategory EventCategory => EventCategory.System;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "MISSION_ENGINE";

    public required string ObjectiveId { get; init; }
}

public sealed record MissionCompletedEvent : DomainEvent
{
    public override string EventType => EventTypes.System.MissionCompleted;
    public override EventCategory EventCategory => EventCategory.System;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "MISSION_ENGINE";
}

public sealed record ScoreCalculatedEvent : DomainEvent
{
    public override string EventType => "SCORE_CALCULATED";
    public override EventCategory EventCategory => EventCategory.System;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "SCORING_ENGINE";

    public required int TotalScore { get; init; }
}

public sealed record RankAssignedEvent : DomainEvent
{
    public override string EventType => "RANK_ASSIGNED";
    public override EventCategory EventCategory => EventCategory.System;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "SCORING_ENGINE";

    public required string Rank { get; init; }
}

public sealed record MissionEvaluationCompletedEvent : DomainEvent
{
    public override string EventType => "MISSION_EVALUATION_COMPLETED";
    public override EventCategory EventCategory => EventCategory.System;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "SCORING_ENGINE";

    public required string MissionResult { get; init; }
}

public sealed record AchievementUnlockedEvent : DomainEvent
{
    public override string EventType => "ACHIEVEMENT_UNLOCKED";
    public override EventCategory EventCategory => EventCategory.System;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "ACHIEVEMENT_ENGINE";

    public required string AchievementId { get; init; }
}

public sealed record PlayerLevelChangedEvent : DomainEvent
{
    public override string EventType => "PLAYER_LEVEL_CHANGED";
    public override EventCategory EventCategory => EventCategory.System;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "ACHIEVEMENT_ENGINE";

    public required int OldLevel { get; init; }
    public required int NewLevel { get; init; }
}

public sealed record ExperienceAwardedEvent : DomainEvent
{
    public override string EventType => "EXPERIENCE_AWARDED";
    public override EventCategory EventCategory => EventCategory.System;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "ACHIEVEMENT_ENGINE";

    public required int Amount { get; init; }
    public required string Reason { get; init; }
}
