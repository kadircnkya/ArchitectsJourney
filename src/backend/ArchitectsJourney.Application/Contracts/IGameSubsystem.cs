using ArchitectsJourney.Application.Common;
using ArchitectsJourney.Application.Events;

namespace ArchitectsJourney.Application.Contracts;

/// <summary>
/// Result returned when a subsystem initializes for a new session.
/// </summary>
public sealed record SubsystemInitResult(bool Success, string? FailureReason = null)
{
    public static SubsystemInitResult Ok() => new(true);
    public static SubsystemInitResult Failed(string reason) => new(false, reason);
}

/// <summary>
/// Result returned when a subsystem handles a domain event.
/// </summary>
public sealed record EventHandlingResult(bool Acknowledged, string? FailureReason = null)
{
    public static EventHandlingResult Ack() => new(true);
    public static EventHandlingResult Nack(string reason) => new(false, reason);
}

/// <summary>
/// An immutable snapshot of a subsystem's state at a specific point in time.
/// Used by the checkpoint and restore protocol.
/// </summary>
public sealed record SubsystemSnapshot
{
    public required string SubsystemId { get; init; }
    public required Guid SessionId { get; init; }
    public required ulong SequenceNumber { get; init; }
    public required DateTimeOffset CapturedAt { get; init; }

    /// <summary>
    /// Serialized state payload. Subsystem-specific format.
    /// The subsystem is responsible for serialization and deserialization.
    /// </summary>
    public required string StateJson { get; init; }
}

/// <summary>
/// Base contract that every specialized engine must implement.
/// Enables the Game Engine to coordinate all subsystems through abstraction.
/// The Game Engine holds only IGameSubsystem references — never concrete engine types.
/// Defined in Document 09, Section 12.
/// </summary>
public interface IGameSubsystem
{
    /// <summary>
    /// Unique identifier for this subsystem.
    /// Used in the Publisher Registry and Subscriber Registry of the Event Bus.
    /// Must be stable across application restarts.
    /// </summary>
    string SubsystemId { get; }

    /// <summary>
    /// Initializes the subsystem for a new session context.
    /// Called by the Game Engine during SESSION_CREATION.
    /// Subsystem must be ready to process events after this returns successfully.
    /// </summary>
    Task<SubsystemInitResult> InitializeAsync(
        SessionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delivers a domain event to this subsystem.
    /// The subsystem must complete all processing and confirm before returning.
    /// Returning a Nack from a REQUIRED subscriber triggers the rollback protocol.
    /// </summary>
    Task<EventHandlingResult> OnEventAsync(
        DomainEvent @event,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases all session-scoped resources.
    /// Called by the Game Engine during SESSION_END.
    /// </summary>
    Task ReleaseAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
