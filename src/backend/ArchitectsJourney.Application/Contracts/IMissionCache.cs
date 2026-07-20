using ArchitectsJourney.Domain.Aggregates;

namespace ArchitectsJourney.Application.Contracts;

public interface IMissionCache
{
    bool TryGetMission(string missionId, out Mission? mission);
    void CacheMission(Mission mission);
}
