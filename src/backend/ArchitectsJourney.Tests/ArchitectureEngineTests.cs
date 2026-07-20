using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Application.Events;
using ArchitectsJourney.Application.Events.Architecture;
using ArchitectsJourney.Domain.Aggregates;
using ArchitectsJourney.Engines.Architecture;
using ArchitectsJourney.Infrastructure.Architecture;
using ArchitectsJourney.Infrastructure.EventTracker;
using ArchitectsJourney.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ArchitectsJourney.Tests;

public class ArchitectureEngineTests
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
    public async Task OnEventAsync_ValidMutation_AppliesAndPublishesEvent()
    {
        // Arrange
        var playthroughRepo = new InMemoryPlaythroughRepository();
        var eventBus = new StubEventBus();
        var validator = new ArchitectureValidator(new ArchitectureValidationOptions { RequireStrictTechAssignments = false });
        var tracker = new InMemoryEventIdempotencyTracker();

        var engine = new ArchitectureEngine(
            NullLogger<ArchitectureEngine>.Instance,
            eventBus,
            playthroughRepo,
            validator,
            tracker
        );

        var playthroughId = Guid.NewGuid();
        var playthrough = new Playthrough(playthroughId, "M1");
        await playthroughRepo.SaveAsync(playthrough);

        var @event = new ArchitectureChangedEvent
        {
            EventId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            PlaythroughId = playthroughId,
            MissionId = "M1",
            CorrelationId = Guid.NewGuid(),
            MutationType = "AddNode",
            TargetId = "NodeA|Service|Backend"
        };

        // Act
        var result = await engine.OnEventAsync(@event);

        // Assert
        Assert.True(result.Acknowledged);
        var updatedPlaythrough = await playthroughRepo.GetByIdAsync(playthroughId);
        Assert.Single(updatedPlaythrough!.Nodes);
        Assert.Equal("NodeA", updatedPlaythrough.Nodes[0].Id);
        Assert.Single(eventBus.PublishedEvents);
        Assert.IsType<ArchitectureNodeAddedEvent>(eventBus.PublishedEvents[0]);
    }

    [Fact]
    public async Task OnEventAsync_ValidationFails_DoesNotMutateOrPublish()
    {
        // Arrange
        var playthroughRepo = new InMemoryPlaythroughRepository();
        var eventBus = new StubEventBus();
        var validator = new ArchitectureValidator(new ArchitectureValidationOptions { RequireStrictTechAssignments = true });
        var tracker = new InMemoryEventIdempotencyTracker();

        var engine = new ArchitectureEngine(
            NullLogger<ArchitectureEngine>.Instance,
            eventBus,
            playthroughRepo,
            validator,
            tracker
        );

        var playthroughId = Guid.NewGuid();
        var playthrough = new Playthrough(playthroughId, "M1");
        await playthroughRepo.SaveAsync(playthrough);

        var @event = new ArchitectureChangedEvent
        {
            EventId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            PlaythroughId = playthroughId,
            MissionId = "M1",
            CorrelationId = Guid.NewGuid(),
            MutationType = "AddNode",
            TargetId = "NodeA|Service|Backend|" // Missing technology, strict is on
        };

        // Act
        var result = await engine.OnEventAsync(@event);

        // Assert
        Assert.False(result.Acknowledged); // Nack
        var updatedPlaythrough = await playthroughRepo.GetByIdAsync(playthroughId);
        Assert.Empty(updatedPlaythrough!.Nodes);
        Assert.Empty(eventBus.PublishedEvents); // No events published
    }

    [Fact]
    public async Task OnEventAsync_DuplicateEvent_Ignored()
    {
        // Arrange
        var playthroughRepo = new InMemoryPlaythroughRepository();
        var eventBus = new StubEventBus();
        var validator = new ArchitectureValidator(new ArchitectureValidationOptions());
        var tracker = new InMemoryEventIdempotencyTracker();

        var engine = new ArchitectureEngine(
            NullLogger<ArchitectureEngine>.Instance,
            eventBus,
            playthroughRepo,
            validator,
            tracker
        );

        var playthroughId = Guid.NewGuid();
        var playthrough = new Playthrough(playthroughId, "M1");
        await playthroughRepo.SaveAsync(playthrough);

        var @event = new ArchitectureChangedEvent
        {
            EventId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            PlaythroughId = playthroughId,
            MissionId = "M1",
            CorrelationId = Guid.NewGuid(),
            MutationType = "AddNode",
            TargetId = "NodeA|Service|Backend"
        };

        // Act
        await engine.OnEventAsync(@event);
        await engine.OnEventAsync(@event); // Duplicate

        // Assert
        var updatedPlaythrough = await playthroughRepo.GetByIdAsync(playthroughId);
        Assert.Single(updatedPlaythrough!.Nodes); // Only added once
        Assert.Single(eventBus.PublishedEvents); // Published once
    }
}
