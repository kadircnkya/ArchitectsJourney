using ArchitectsJourney.Application.Contracts;
using System.Collections.Concurrent;

namespace ArchitectsJourney.Infrastructure.Persistence;

/// <summary>
/// Thread-safe in-memory implementation of the Save System checkpoint store.
/// </summary>
public sealed class InMemorySaveSystem : ISaveSystem
{
    private readonly ConcurrentDictionary<Guid, CheckpointRecord> _checkpoints = new();

    private sealed record CheckpointRecord(
        Guid SessionId,
        ulong SequenceNumber,
        IReadOnlyList<SubsystemSnapshot> Snapshots,
        DateTimeOffset SavedAt);

    public Task SaveCheckpointAsync(
        Guid sessionId,
        ulong sequenceNumber,
        IReadOnlyList<SubsystemSnapshot> snapshots,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshots);
        _checkpoints[sessionId] = new CheckpointRecord(sessionId, sequenceNumber, snapshots, DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SubsystemSnapshot>?> LoadLastCheckpointAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        if (_checkpoints.TryGetValue(sessionId, out var record))
        {
            return Task.FromResult<IReadOnlyList<SubsystemSnapshot>?>(record.Snapshots);
        }
        return Task.FromResult<IReadOnlyList<SubsystemSnapshot>?>(null);
    }
}
