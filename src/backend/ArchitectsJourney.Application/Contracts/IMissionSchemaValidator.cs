namespace ArchitectsJourney.Application.Contracts;

public interface IMissionSchemaValidator
{
    Task ValidateAsync(string missionId, string jsonPayload, CancellationToken cancellationToken = default);
}
