using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Domain.Exceptions;

namespace ArchitectsJourney.Infrastructure.MissionLoading;

public sealed class MissionDiscovery : IMissionDiscovery
{
    private readonly string _basePath;

    public MissionDiscovery()
    {
        // For MVP, look in a "Missions" directory relative to execution
        _basePath = Path.Combine(AppContext.BaseDirectory, "Missions");
    }

    public Task<string> GetMissionFilePathAsync(string missionId, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_basePath, $"{missionId}.json");
        if (!File.Exists(filePath))
        {
            throw MissionNotFoundException.ForMissionId(missionId);
        }

        return Task.FromResult(filePath);
    }
}
