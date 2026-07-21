using ArchitectsJourney.Application.Common;
using ArchitectsJourney.Application.Events;

namespace ArchitectsJourney.Application.Events.Metric;

public sealed record MetricDeltaAppliedEvent : DomainEvent
{
    public override string EventType => EventTypes.Metric.DeltaApplied;
    public override EventCategory EventCategory => EventCategory.Metric;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "RULE_ENGINE";

    public required string Metric { get; init; }
    public required int Value { get; init; }
}

public sealed record MetricThresholdCrossedEvent : DomainEvent
{
    public override string EventType => EventTypes.Metric.ThresholdCrossed;
    public override EventCategory EventCategory => EventCategory.Metric;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "METRIC_ENGINE";

    public required string Metric { get; init; }
    public required int ThresholdValue { get; init; }
    public required bool CrossedUpwards { get; init; }
}

public sealed record MetricBoundsEnforcedEvent : DomainEvent
{
    public override string EventType => EventTypes.Metric.BoundsEnforced;
    public override EventCategory EventCategory => EventCategory.Metric;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "METRIC_ENGINE";

    public required string Metric { get; init; }
    public required int AttemptedValue { get; init; }
    public required int EnforcedValue { get; init; }
}

public sealed record MetricsUpdatedEvent : DomainEvent
{
    public override string EventType => EventTypes.Metric.Updated;
    public override EventCategory EventCategory => EventCategory.Metric;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "METRIC_ENGINE";
}

