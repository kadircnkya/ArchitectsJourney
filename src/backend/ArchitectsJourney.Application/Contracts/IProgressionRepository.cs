using ArchitectsJourney.Application.Common;

namespace ArchitectsJourney.Application.Contracts;

/// <summary>
/// Contract representing the player progression state.
/// Maps to CareerRank and progression concepts in Document 05.
/// </summary>
public sealed record PlayerProgressSnapshot
{
    public required Guid PlayerId { get; init; }
    public required string CareerRank { get; init; }
    public required int CompletedMissionsCount { get; init; }
    public required IReadOnlyList<string> UnlockedMissions { get; init; }
    public required IReadOnlyList<string> UnlockedTechnologies { get; init; }
}

public interface IProgressionRepository
{
    Task<PlayerProgressSnapshot?> GetProgressAsync(
        Guid playerId,
        CancellationToken cancellationToken = default);

    Task SaveProgressAsync(
        PlayerProgressSnapshot progress,
        CancellationToken cancellationToken = default);
}
