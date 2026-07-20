using ArchitectsJourney.Application.DTOs.Mission;
using ArchitectsJourney.Domain.Aggregates;

namespace ArchitectsJourney.Application.Contracts;

public interface IMissionBuilder
{
    Mission Build(MissionDto dto);
}
