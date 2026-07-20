using ArchitectsJourney.Application.Common;
using ArchitectsJourney.Application.Events;

namespace ArchitectsJourney.Application.Events.Player;

public sealed record PlayerDecisionSubmittedEvent : DomainEvent
{
    public override string EventType => EventTypes.Player.DecisionSubmitted;
    public override EventCategory EventCategory => EventCategory.Player;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "UI_LAYER";

    public required string DecisionPointId { get; init; }
    public required string SelectedOptionId { get; init; }
}

public sealed record PlayerQuestionAskedEvent : DomainEvent
{
    public override string EventType => EventTypes.Player.QuestionAsked;
    public override EventCategory EventCategory => EventCategory.Player;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "UI_LAYER";

    public required string QuestionId { get; init; }
}
