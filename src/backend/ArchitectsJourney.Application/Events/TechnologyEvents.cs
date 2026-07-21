using ArchitectsJourney.Application.Common;
using ArchitectsJourney.Application.Events;

namespace ArchitectsJourney.Application.Events.Technology;

public sealed record TechnologyDiscoveredEvent : DomainEvent
{
    public override string EventType => EventTypes.Technology.Discovered;
    public override EventCategory EventCategory => EventCategory.Technology;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "RULE_ENGINE";

    public required string TechnologyId { get; init; }
}

public sealed record TechnologyUnlockedEvent : DomainEvent
{
    public override string EventType => EventTypes.Technology.Unlocked;
    public override EventCategory EventCategory => EventCategory.Technology;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "TECHNOLOGY_ENGINE";

    public required string TechnologyId { get; init; }
}

public sealed record TechnologyConflictDetectedEvent : DomainEvent
{
    public override string EventType => EventTypes.Technology.ConflictDetected;
    public override EventCategory EventCategory => EventCategory.Technology;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "TECHNOLOGY_ENGINE";

    public required string NodeId { get; init; }
    public required string TechnologyId { get; init; }
    public required string ConflictingTechnologyId { get; init; }
}

public sealed record UnavailableTechnologyUsedEvent : DomainEvent
{
    public override string EventType => EventTypes.Technology.UnavailableUsed;
    public override EventCategory EventCategory => EventCategory.Technology;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "TECHNOLOGY_ENGINE";

    public required string NodeId { get; init; }
    public required string TechnologyId { get; init; }
}
