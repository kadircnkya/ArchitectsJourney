using ArchitectsJourney.Application.Events;

namespace ArchitectsJourney.Application.Contracts;

/// <summary>
/// Defines a registered subscriber configuration.
/// Defined in Document 10, Section 11.1.
/// </summary>
public sealed record SubscriptionRegistration
{
    public required string SubscriberId { get; init; }
    public required SubscriptionType Type { get; init; }
    public string? TargetEventType { get; init; }
    public EventCategory? TargetCategory { get; init; }
    public bool RequiresAcknowledgement { get; init; } = true;
    public int DeliveryOrder { get; init; }
    public int TimeoutMilliseconds { get; init; } = 5000;
}

public enum SubscriptionType
{
    Type,
    Category,
    Wildcard
}

/// <summary>
/// Defines a registered publisher configuration.
/// Defined in Document 10, Section 12.1.
/// </summary>
public sealed record PublisherRegistration
{
    public required string PublisherId { get; init; }
    public required IReadOnlyList<string> AuthorizedEventTypes { get; init; }
}
