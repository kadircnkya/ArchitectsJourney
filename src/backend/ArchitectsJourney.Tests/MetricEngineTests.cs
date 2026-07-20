using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Application.Events.Metric;
using ArchitectsJourney.Application.Events.Rule;
using ArchitectsJourney.Domain.Aggregates;
using ArchitectsJourney.Domain.Enums;
using ArchitectsJourney.Engines.Metric;
using ArchitectsJourney.Infrastructure.EventBus;
using ArchitectsJourney.Infrastructure.EventTracker;
using ArchitectsJourney.Infrastructure.Metrics;
using ArchitectsJourney.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using ArchitectsJourney.Application.Events;
using ArchitectsJourney.Application.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ArchitectsJourney.Tests;

public class MetricEngineTests
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
    public async Task MetricEngine_HandlesMetricDeltaAppliedEvent_AppliesDeltaAndBounds()
    {
        // Arrange
        var playthroughRepo = new InMemoryPlaythroughRepository();
        var missionRepo = new InMemoryMissionRepository();
        var eventBus = new StubEventBus();
        
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?> {
            {"MetricEngine:Thresholds:0", "25"},
            {"MetricEngine:Thresholds:1", "50"},
            {"MetricEngine:Thresholds:2", "75"}
        });
        var metricConfig = new MetricConfiguration(configBuilder.Build());
        var tracker = new InMemoryEventIdempotencyTracker();

        var engine = new MetricEngine(
            NullLogger<MetricEngine>.Instance,
            eventBus,
            playthroughRepo,
            missionRepo,
            metricConfig,
            tracker
        );

        var playthroughId = Guid.NewGuid();
        var playthrough = new Playthrough(playthroughId, "M1");
        playthrough.SetMetricValue(MetricType.Cost, 10);
        await playthroughRepo.SaveAsync(playthrough);

        var @event = new MetricDeltaAppliedEvent
        {
            EventId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            PlaythroughId = playthroughId,
            MissionId = "M1",
            CorrelationId = Guid.NewGuid(),
            Metric = "Cost",
            Value = 5
        };

        // Act
        var result = await engine.OnEventAsync(@event);

        // Assert
        Assert.True(result.Acknowledged);
        var updatedPlaythrough = await playthroughRepo.GetByIdAsync(playthroughId);
        Assert.Equal(15, updatedPlaythrough!.Metrics[MetricType.Cost]);
        Assert.Single(updatedPlaythrough.MetricHistory);
    }

    [Fact]
    public async Task MetricEngine_PreventsDuplicateEvents()
    {
        // Arrange
        var playthroughRepo = new InMemoryPlaythroughRepository();
        var tracker = new InMemoryEventIdempotencyTracker();
        var configBuilder = new ConfigurationBuilder();
        var metricConfig = new MetricConfiguration(configBuilder.Build());

        var engine = new MetricEngine(
            NullLogger<MetricEngine>.Instance,
            new StubEventBus(),
            playthroughRepo,
            new InMemoryMissionRepository(),
            metricConfig,
            tracker
        );

        var playthroughId = Guid.NewGuid();
        var playthrough = new Playthrough(playthroughId, "M1");
        playthrough.SetMetricValue(MetricType.Cost, 10);
        await playthroughRepo.SaveAsync(playthrough);

        var @event = new MetricDeltaAppliedEvent
        {
            EventId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            PlaythroughId = playthroughId,
            MissionId = "M1",
            CorrelationId = Guid.NewGuid(),
            Metric = "Cost",
            Value = 10
        };

        // Act
        await engine.OnEventAsync(@event);
        await engine.OnEventAsync(@event); // Duplicate

        // Assert
        var updatedPlaythrough = await playthroughRepo.GetByIdAsync(playthroughId);
        Assert.Equal(20, updatedPlaythrough!.Metrics[MetricType.Cost]); // Only applied once
        Assert.Single(updatedPlaythrough.MetricHistory);
    }
}
