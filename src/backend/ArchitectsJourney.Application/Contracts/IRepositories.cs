using ArchitectsJourney.Application.Common;
using ArchitectsJourney.Domain.Aggregates;

namespace ArchitectsJourney.Application.Contracts;

public interface IMissionRepository
{
    Task<Mission?> GetByIdAsync(string missionId, CancellationToken cancellationToken = default);
    Task SaveAsync(Mission mission, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Mission>> ListAllAsync(CancellationToken cancellationToken = default);
}

public interface IPlaythroughRepository
{
    Task<Playthrough?> GetByIdAsync(Guid playthroughId, CancellationToken cancellationToken = default);
    Task SaveAsync(Playthrough playthrough, CancellationToken cancellationToken = default);
}
