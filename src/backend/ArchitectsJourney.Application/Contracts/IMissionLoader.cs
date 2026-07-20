using ArchitectsJourney.Domain.Aggregates;

namespace ArchitectsJourney.Application.Contracts;

public interface IMissionLoader
{
    Task<Mission> LoadMissionAsync(string missionId, CancellationToken cancellationToken = default);
}
