using ArchitectsJourney.Application.Common;
using ArchitectsJourney.Application.Events;

namespace ArchitectsJourney.Application.Events.Rule;

public sealed record DecisionProcessedEvent : DomainEvent
{
    public override string EventType => EventTypes.Rule.DecisionProcessed;
    public override EventCategory EventCategory => EventCategory.Rule;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "RULE_ENGINE";

    public required string DecisionPointId { get; init; }
    public required string OptionId { get; init; }
    public required IReadOnlyList<string> AppliedRuleIds { get; init; }
    public required bool PhaseCompleted { get; init; }
}

public sealed record QuestionProcessedEvent : DomainEvent
{
    public override string EventType => EventTypes.Rule.QuestionProcessed;
    public override EventCategory EventCategory => EventCategory.Rule;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "RULE_ENGINE";

    public required string QuestionId { get; init; }
    public required bool MechanicalEffectApplied { get; init; }
}

public sealed record DerivedRuleTriggeredEvent : DomainEvent
{
    public override string EventType => EventTypes.Rule.DerivedRuleTriggered;
    public override EventCategory EventCategory => EventCategory.Rule;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "RULE_ENGINE";

    public required string OriginatingRuleId { get; init; }
    public required string DerivedRuleId { get; init; }
    public required int Depth { get; init; }
}

public sealed record RuleExecutionAuditCreatedEvent : DomainEvent
{
    public override string EventType => EventTypes.Rule.RuleExecutionAuditCreated;
    public override EventCategory EventCategory => EventCategory.Rule;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "RULE_ENGINE";

    public required string RuleId { get; init; }
    public required string Explanation { get; init; }
}

public sealed record BusinessEventProcessedEvent : DomainEvent
{
    public override string EventType => EventTypes.Rule.BusinessEventProcessed;
    public override EventCategory EventCategory => EventCategory.Rule;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "RULE_ENGINE";

    public required string BusinessEventName { get; init; }
}
