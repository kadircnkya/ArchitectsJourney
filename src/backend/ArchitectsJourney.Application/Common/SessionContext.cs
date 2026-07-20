namespace ArchitectsJourney.Application.Common;

/// <summary>
/// The correlation context for an active game session.
/// Passed to subsystems during initialization and attached to all events
/// produced within this session.
/// Defined in Document 09, Section 4.2.
/// </summary>
public sealed record SessionContext
{
    public required Guid SessionId { get; init; }
    public required Guid PlaythroughId { get; init; }
    public required string MissionId { get; init; }
    public required Guid PlayerId { get; init; }
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;
}
