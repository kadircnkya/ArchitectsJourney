using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Domain.Aggregates;

namespace ArchitectsJourney.Infrastructure.Persistence;

/// <summary>
/// Simple in-memory mock repository implementing the persistence interfaces for MVP.
/// </summary>
public sealed class InMemoryMissionRepository : IMissionRepository
{
    private readonly Dictionary<string, Mission> _missions = new();

    public Task<Mission?> GetByIdAsync(string missionId, CancellationToken cancellationToken = default)
    {
        _missions.TryGetValue(missionId, out var mission);
        return Task.FromResult(mission);
    }

    public Task SaveAsync(Mission mission, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(mission);
        _missions[mission.Id] = mission;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Mission>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Mission> list = _missions.Values.ToList();
        return Task.FromResult(list);
    }
}

public sealed class InMemoryPlaythroughRepository : IPlaythroughRepository
{
    private readonly Dictionary<Guid, Playthrough> _playthroughs = new();

    public Task<Playthrough?> GetByIdAsync(Guid playthroughId, CancellationToken cancellationToken = default)
    {
        _playthroughs.TryGetValue(playthroughId, out var playthrough);
        return Task.FromResult(playthrough);
    }

    public Task SaveAsync(Playthrough playthrough, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(playthrough);
        _playthroughs[playthrough.Id] = playthrough;
        return Task.CompletedTask;
    }
}
