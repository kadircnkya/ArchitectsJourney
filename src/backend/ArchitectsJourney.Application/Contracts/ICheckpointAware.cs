using ArchitectsJourney.Application.Contracts;

namespace ArchitectsJourney.Application.Contracts;

/// <summary>
/// Implemented by stateful subsystems that participate in the checkpoint/restore protocol.
/// Separates checkpoint responsibility from the general IGameSubsystem lifecycle
/// so that stateless subsystems are not forced to implement it.
/// Defined in Document 09, Section 12.3 (ICheckpointAware).
/// </summary>
public interface ICheckpointAware
{
    /// <summary>
    /// Produces an immutable snapshot of the current subsystem state.
    /// Called by the Game Engine during PHASE 10 (Checkpoint) of the game loop.
    /// The snapshot must be sufficient to restore the subsystem to this exact state.
    /// </summary>
    Task<SubsystemSnapshot> TakeSnapshotAsync(Guid playthroughId, Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores the subsystem state from a previously captured snapshot.
    /// Called during rollback protocol or session restore.
    /// After this returns, the subsystem must behave as if it had just reached
    /// the state at snapshot capture time.
    /// </summary>
    Task RestoreFromSnapshotAsync(
        SubsystemSnapshot snapshot,
        CancellationToken cancellationToken = default);
}
