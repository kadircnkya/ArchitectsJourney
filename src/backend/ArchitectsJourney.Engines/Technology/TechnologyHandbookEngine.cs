using System;
using System.Threading;
using System.Threading.Tasks;
using ArchitectsJourney.Application.Common;
using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Application.Events;
using ArchitectsJourney.Application.Events.Architecture;
using ArchitectsJourney.Application.Events.Technology;
using ArchitectsJourney.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ArchitectsJourney.Engines.Technology;

public sealed class TechnologyHandbookEngine : IGameSubsystem
{
    private readonly ILogger<TechnologyHandbookEngine> _logger;
    private readonly IPlaythroughRepository _playthroughRepository;
    private readonly IEventBus _eventBus;
    private readonly ITechnologyCatalog _technologyCatalog;
    private readonly ITechnologyValidator _technologyValidator;
    private readonly IEventIdempotencyTracker _idempotencyTracker;

    public TechnologyHandbookEngine(
        ILogger<TechnologyHandbookEngine> logger,
        IEventBus eventBus,
        IPlaythroughRepository playthroughRepository,
        ITechnologyCatalog technologyCatalog,
        ITechnologyValidator technologyValidator,
        IEventIdempotencyTracker idempotencyTracker)
    {
        _logger = logger;
        _eventBus = eventBus;
        _playthroughRepository = playthroughRepository;
        _technologyCatalog = technologyCatalog;
        _technologyValidator = technologyValidator;
        _idempotencyTracker = idempotencyTracker;
    }

    public string SubsystemId => "TECHNOLOGY_ENGINE";

    public Task<SubsystemInitResult> InitializeAsync(SessionContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(SubsystemInitResult.Ok());
    }

    public Task ReleaseAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task<EventHandlingResult> OnEventAsync(DomainEvent @event, CancellationToken cancellationToken = default)
    {
        switch (@event)
        {
            case TechnologyDiscoveredEvent discoveredEvent:
                return await HandleTechnologyDiscoveredAsync(discoveredEvent, cancellationToken);
            case ArchitectureChangedEvent changedEvent when changedEvent.MutationType == "UpdateTechnology":
                var parts = changedEvent.TargetId.Split('|');
                var technologyId = parts.Length > 1 ? parts[1] : null;
                return await HandleArchitectureMutationAsync(changedEvent, technologyId, cancellationToken);
            default:
                return EventHandlingResult.Ack();
        }
    }

    private async Task<EventHandlingResult> HandleTechnologyDiscoveredAsync(TechnologyDiscoveredEvent @event, CancellationToken cancellationToken)
    {
        // 1. Check Idempotency
        if (!@event.PlaythroughId.HasValue) return EventHandlingResult.Ack();
        if (!await _idempotencyTracker.TryMarkProcessedAsync(@event.PlaythroughId.Value, @event.EventId, cancellationToken))
        {
            _logger.LogInformation("[TechnologyEngine] Duplicate TechnologyDiscoveredEvent {EventId} ignored.", @event.EventId);
            return EventHandlingResult.Ack();
        }

        // 2. Validate Technology Exists
        var technology = await _technologyCatalog.GetTechnologyAsync(@event.TechnologyId, cancellationToken);
        if (technology == null)
        {
            _logger.LogWarning("[TechnologyEngine] Attempted to discover unknown technology {TechnologyId}.", @event.TechnologyId);
            return EventHandlingResult.Nack("Unknown technology");
        }

        // 3. Load Playthrough
        var playthrough = await _playthroughRepository.GetByIdAsync(@event.PlaythroughId.Value, cancellationToken);
        if (playthrough == null)
        {
            return EventHandlingResult.Nack("Playthrough not found");
        }

        // 4. Mutate State
        playthrough.DiscoverTechnology(@event.TechnologyId);

        // 5. Save Playthrough
        await _playthroughRepository.SaveAsync(playthrough, cancellationToken);

        // 6. Publish Event
        var unlockedEvent = new TechnologyUnlockedEvent
        {
            EventId = Guid.NewGuid(),
            SessionId = @event.SessionId,
            PlaythroughId = @event.PlaythroughId,
            MissionId = @event.MissionId,
            CorrelationId = @event.CorrelationId,
            TechnologyId = @event.TechnologyId
        };

        await _eventBus.PublishAsync(unlockedEvent, cancellationToken);
        _logger.LogInformation("[TechnologyEngine] Discovered Technology {TechnologyId} successfully.", @event.TechnologyId);

        return EventHandlingResult.Ack();
    }

    private async Task<EventHandlingResult> HandleArchitectureMutationAsync(DomainEvent @event, string? technologyId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(technologyId)) return EventHandlingResult.Ack();

        // 1. Validate Technology Exists
        var technology = await _technologyCatalog.GetTechnologyAsync(technologyId, cancellationToken);
        if (technology == null)
        {
            return EventHandlingResult.Ack(); // Architecture Engine's problem, not a technology conflict.
        }

        // 2. Load Playthrough
        if (!@event.PlaythroughId.HasValue) return EventHandlingResult.Ack();
        var playthrough = await _playthroughRepository.GetByIdAsync(@event.PlaythroughId.Value, cancellationToken);
        if (playthrough == null) return EventHandlingResult.Ack();

        string targetNodeId = @event switch
        {
            ArchitectureChangedEvent ac => ac.TargetId.Split('|')[0],
            _ => string.Empty
        };

        // 3. Validate Constraints
        var result = _technologyValidator.ValidateTechnologyUsage(playthrough.Nodes, playthrough.DiscoveredTechnologies, targetNodeId, technology);

        if (!result.IsValid)
        {
            DomainEvent notificationEvent;
            if (result.ConflictingTechnologyId != null)
            {
                notificationEvent = new TechnologyConflictDetectedEvent
                {
                    EventId = Guid.NewGuid(),
                    SessionId = @event.SessionId,
                    PlaythroughId = @event.PlaythroughId,
                    MissionId = @event.MissionId,
                    CorrelationId = @event.CorrelationId,
                    NodeId = targetNodeId,
                    TechnologyId = technologyId,
                    ConflictingTechnologyId = result.ConflictingTechnologyId
                };
                _logger.LogWarning("[TechnologyEngine] Conflict detected for {TechnologyId} on node {NodeId}", technologyId, targetNodeId);
            }
            else
            {
                notificationEvent = new UnavailableTechnologyUsedEvent
                {
                    EventId = Guid.NewGuid(),
                    SessionId = @event.SessionId,
                    PlaythroughId = @event.PlaythroughId,
                    MissionId = @event.MissionId,
                    CorrelationId = @event.CorrelationId,
                    NodeId = targetNodeId,
                    TechnologyId = technologyId
                };
                _logger.LogWarning("[TechnologyEngine] Unavailable technology {TechnologyId} used on node {NodeId}", technologyId, targetNodeId);
            }

            // Publish validation event
            await _eventBus.PublishAsync(notificationEvent, cancellationToken);
        }

        return EventHandlingResult.Ack();
    }
}
