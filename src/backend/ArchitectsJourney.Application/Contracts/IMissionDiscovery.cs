namespace ArchitectsJourney.Application.Contracts;

public interface IMissionDiscovery
{
    Task<string> GetMissionFilePathAsync(string missionId, CancellationToken cancellationToken = default);
}
