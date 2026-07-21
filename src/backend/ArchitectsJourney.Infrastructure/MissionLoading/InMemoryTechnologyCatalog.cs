using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Domain.Entities;
using DomainTechnology = ArchitectsJourney.Domain.Entities.Technology;

namespace ArchitectsJourney.Infrastructure.MissionLoading;

public sealed class InMemoryTechnologyCatalog : ITechnologyCatalog
{
    private readonly Dictionary<string, DomainTechnology> _technologies;

    public InMemoryTechnologyCatalog()
    {
        _technologies = new Dictionary<string, DomainTechnology>(StringComparer.OrdinalIgnoreCase);

        var microservices = new DomainTechnology("tech_microservices", "Microservices", "Architecture");
        var eventDriven = new DomainTechnology("tech_event_driven", "Event-Driven", "Architecture");
        var serverless = new DomainTechnology("tech_serverless", "Serverless", "Architecture");
        var nosql = new DomainTechnology("tech_nosql", "NoSQL", "Database");
        var cqrs = new DomainTechnology("tech_cqrs", "CQRS", "Pattern");
        var monolithicDb = new DomainTechnology("tech_monolithic_db", "Monolithic Database", "Database");

        // Example relationships
        cqrs.AddPrerequisite("tech_event_driven");
        microservices.AddConflict("tech_monolithic_db");

        _technologies.Add(microservices.Id, microservices);
        _technologies.Add(eventDriven.Id, eventDriven);
        _technologies.Add(serverless.Id, serverless);
        _technologies.Add(nosql.Id, nosql);
        _technologies.Add(cqrs.Id, cqrs);
        _technologies.Add(monolithicDb.Id, monolithicDb);
    }

    public Task<DomainTechnology?> GetTechnologyAsync(string technologyId, CancellationToken cancellationToken = default)
    {
        _technologies.TryGetValue(technologyId, out var technology);
        return Task.FromResult(technology);
    }
}
