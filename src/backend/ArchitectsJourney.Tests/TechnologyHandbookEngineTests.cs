using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Application.Events;
using ArchitectsJourney.Application.Events.Architecture;
using ArchitectsJourney.Application.Events.Technology;
using ArchitectsJourney.Domain.Aggregates;
using ArchitectsJourney.Domain.Entities;
using ArchitectsJourney.Engines.Technology;
using ArchitectsJourney.Infrastructure.EventTracker;
using ArchitectsJourney.Infrastructure.MissionLoading;
using ArchitectsJourney.Infrastructure.Persistence;
using ArchitectsJourney.Infrastructure.Technology;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ArchitectsJourney.Tests;

public class TechnologyHandbookEngineTests
{
    private class StubEventBus : IEventBus
    {
        public List<DomainEvent> PublishedEvents { get; } = new();

        public Task<PublicationResult> PublishAsync(DomainEvent @event, CancellationToken cancellationToken = default)
        {
            PublishedEvents.Add(@event);
            return Task.FromResult(new PublicationResult { Accepted = true, EventId = @event.EventId, SequenceNumber = (ulong)PublishedEvents.Count });
        }

        public void RegisterPublisher(PublisherRegistration registration) { }
        public void RegisterSubscriber(SubscriptionRegistration registration) { }
    }

    [Fact]
    public async Task HandleTechnologyDiscovered_Valid_MutatesAndPublishes()
    {
        // Arrange
        var repo = new InMemoryPlaythroughRepository();
        var eventBus = new StubEventBus();
        var catalog = new InMemoryTechnologyCatalog();
        var validator = new TechnologyValidator();
        var tracker = new InMemoryEventIdempotencyTracker();

        var engine = new TechnologyHandbookEngine(
            NullLogger<TechnologyHandbookEngine>.Instance,
            eventBus,
            repo,
            catalog,
            validator,
            tracker
        );

        var playthroughId = Guid.NewGuid();
        var playthrough = new Playthrough(playthroughId, "M1");
        await repo.SaveAsync(playthrough);

        var @event = new TechnologyDiscoveredEvent
        {
            EventId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            PlaythroughId = playthroughId,
            MissionId = "M1",
            CorrelationId = Guid.NewGuid(),
            TechnologyId = "tech_microservices"
        };

        // Act
        var result = await engine.OnEventAsync(@event);

        // Assert
        Assert.True(result.Acknowledged);
        var updated = await repo.GetByIdAsync(playthroughId);
        Assert.Contains("tech_microservices", updated!.DiscoveredTechnologies);
        Assert.Single(eventBus.PublishedEvents);
        Assert.IsType<TechnologyUnlockedEvent>(eventBus.PublishedEvents[0]);
    }

    [Fact]
    public async Task HandleArchitectureMutation_UnavailableTech_PublishesUnavailableEvent()
    {
        // Arrange
        var repo = new InMemoryPlaythroughRepository();
        var eventBus = new StubEventBus();
        var catalog = new InMemoryTechnologyCatalog();
        var validator = new TechnologyValidator();
        var tracker = new InMemoryEventIdempotencyTracker();

        var engine = new TechnologyHandbookEngine(
            NullLogger<TechnologyHandbookEngine>.Instance,
            eventBus,
            repo,
            catalog,
            validator,
            tracker
        );

        var playthroughId = Guid.NewGuid();
        var playthrough = new Playthrough(playthroughId, "M1");
        // Not discovering any tech
        await repo.SaveAsync(playthrough);

        var @event = new ArchitectureChangedEvent
        {
            EventId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            PlaythroughId = playthroughId,
            MissionId = "M1",
            CorrelationId = Guid.NewGuid(),
            MutationType = "UpdateTechnology",
            TargetId = "Node1|tech_microservices"
        };

        // Act
        var result = await engine.OnEventAsync(@event);

        // Assert
        Assert.True(result.Acknowledged);
        Assert.Single(eventBus.PublishedEvents);
        Assert.IsType<UnavailableTechnologyUsedEvent>(eventBus.PublishedEvents[0]);
    }
}
