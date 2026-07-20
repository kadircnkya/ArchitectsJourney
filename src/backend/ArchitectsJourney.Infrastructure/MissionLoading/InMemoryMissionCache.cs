using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Domain.Aggregates;
using System.Collections.Concurrent;

namespace ArchitectsJourney.Infrastructure.MissionLoading;

public sealed class InMemoryMissionCache : IMissionCache
{
    private readonly ConcurrentDictionary<string, Mission> _cache = new(StringComparer.OrdinalIgnoreCase);

    public bool TryGetMission(string missionId, out Mission? mission)
    {
        return _cache.TryGetValue(missionId, out mission);
    }

    public void CacheMission(Mission mission)
    {
        ArgumentNullException.ThrowIfNull(mission);
        _cache[mission.Id] = mission;
    }
}
