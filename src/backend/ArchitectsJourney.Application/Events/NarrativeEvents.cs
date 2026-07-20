using ArchitectsJourney.Application.Common;
using ArchitectsJourney.Application.Events;

namespace ArchitectsJourney.Application.Events.Narrative;

public sealed record FeedbackDeliveredEvent : DomainEvent
{
    public override string EventType => EventTypes.Narrative.FeedbackDelivered;
    public override EventCategory EventCategory => EventCategory.Narrative;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "GAME_ENGINE";

    public required string FeedbackContent { get; init; }
}

public sealed record FeedbackQueuedEvent : DomainEvent
{
    public override string EventType => EventTypes.Narrative.FeedbackQueued;
    public override EventCategory EventCategory => EventCategory.Narrative;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "GAME_ENGINE";

    public required string QueueId { get; init; }
}
