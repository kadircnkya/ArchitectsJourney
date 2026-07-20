namespace ArchitectsJourney.Application.Events;

/// <summary>
/// Priority tiers for Event Bus processing order.
/// Lower numeric value = higher priority.
/// Defined in Document 10, Section 9.
/// </summary>
public enum EventPriority
{
    /// <summary>Session failures, rollback coordination, queue overflow.</summary>
    Critical = 1,

    /// <summary>Session lifecycle, phase transitions, checkpoints, reviews.</summary>
    System = 2,

    /// <summary>Player actions, Rule Engine result events, business events.</summary>
    Domain = 3,

    /// <summary>Technology discovery, handbook updates, metric routine updates.</summary>
    Notification = 4
}
