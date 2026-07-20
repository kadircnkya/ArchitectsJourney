using ArchitectsJourney.Application.Contracts;

namespace ArchitectsJourney.Infrastructure.MissionLoading;

public sealed class InMemoryTechnologyCatalog : ITechnologyCatalog
{
    private readonly HashSet<string> _validTechnologies = new(StringComparer.OrdinalIgnoreCase)
    {
        "tech_microservices",
        "tech_event_driven",
        "tech_serverless",
        "tech_nosql",
        "tech_cqrs"
    };

    public Task<bool> IsValidTechnologyAsync(string technologyId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_validTechnologies.Contains(technologyId));
    }
}
