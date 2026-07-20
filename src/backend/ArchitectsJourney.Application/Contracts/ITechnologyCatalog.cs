namespace ArchitectsJourney.Application.Contracts;

public interface ITechnologyCatalog
{
    Task<bool> IsValidTechnologyAsync(string technologyId, CancellationToken cancellationToken = default);
}
