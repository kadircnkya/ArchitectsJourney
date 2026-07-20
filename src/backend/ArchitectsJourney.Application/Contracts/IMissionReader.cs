namespace ArchitectsJourney.Application.Contracts;

public interface IMissionReader
{
    Task<string> ReadMissionJsonAsync(string filePath, CancellationToken cancellationToken = default);
}
