namespace ArchitectsJourney.Application.Contracts;

public interface ITechnologyCatalog
{
    Task<Domain.Entities.Technology?> GetTechnologyAsync(string technologyId, CancellationToken cancellationToken = default);
}
