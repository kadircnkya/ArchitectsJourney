using ArchitectsJourney.Application.Contracts;

namespace ArchitectsJourney.Infrastructure.MissionLoading;

public sealed class MissionReader : IMissionReader
{
    public async Task<string> ReadMissionJsonAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return await File.ReadAllTextAsync(filePath, cancellationToken);
    }
}
