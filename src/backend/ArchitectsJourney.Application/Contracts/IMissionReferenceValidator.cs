using ArchitectsJourney.Application.DTOs.Mission;

namespace ArchitectsJourney.Application.Contracts;

public interface IMissionReferenceValidator
{
    Task ValidateAsync(string missionId, MissionDto missionDto, CancellationToken cancellationToken = default);
}
