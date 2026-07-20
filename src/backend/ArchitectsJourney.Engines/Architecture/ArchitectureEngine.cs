using System;
using System.Threading;
using System.Threading.Tasks;
using ArchitectsJourney.Application.Common;
using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Application.Events;
using ArchitectsJourney.Application.Events.Architecture;
using ArchitectsJourney.Domain.Entities;
using ArchitectsJourney.Domain.Enums;
using ArchitectsJourney.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ArchitectsJourney.Engines.Architecture;

/// <summary>
/// Architecture graph mutation engine (Phase 6).
/// Stateless orchestrator managing logical architecture node and edge states.
/// </summary>
public sealed class ArchitectureEngine : IGameSubsystem
{
    private readonly ILogger<ArchitectureEngine> _logger;
    private readonly IEventBus _eventBus;
    private readonly IPlaythroughRepository _playthroughRepository;
    private readonly IArchitectureValidator _architectureValidator;
    private readonly IEventIdempotencyTracker _eventIdempotencyTracker;

    public string SubsystemId => "ARCHITECTURE_ENGINE";

    public ArchitectureEngine(
        ILogger<ArchitectureEngine> logger,
        IEventBus eventBus,
        IPlaythroughRepository playthroughRepository,
        IArchitectureValidator architectureValidator,
        IEventIdempotencyTracker eventIdempotencyTracker)
    {
        _logger = logger;
        _eventBus = eventBus;
        _playthroughRepository = playthroughRepository;
        _architectureValidator = architectureValidator;
        _eventIdempotencyTracker = eventIdempotencyTracker;
    }

    public Task<SubsystemInitResult> InitializeAsync(SessionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        _logger.LogInformation("Architecture Graph Engine initialized statelessly for session {SessionId}.", context.SessionId);
        return Task.FromResult(SubsystemInitResult.Ok());
    }

    public async Task<EventHandlingResult> OnEventAsync(DomainEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (@event is ArchitectureChangedEvent architectureChangedEvent)
        {
            if (!@event.PlaythroughId.HasValue)
            {
                return EventHandlingResult.Nack("PlaythroughId missing.");
            }

            try
            {
                return await HandleArchitectureChangedAsync(architectureChangedEvent, @event.PlaythroughId.Value, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Architecture Engine execution failed.");
                return EventHandlingResult.Nack("Architecture Engine error: " + ex.Message);
            }
        }

        return EventHandlingResult.Ack();
    }

    private async Task<EventHandlingResult> HandleArchitectureChangedAsync(ArchitectureChangedEvent @event, Guid playthroughId, CancellationToken cancellationToken)
    {
        // 1. Verify Idempotency
        if (!await _eventIdempotencyTracker.TryMarkProcessedAsync(playthroughId, @event.EventId, cancellationToken))
        {
            _logger.LogWarning("Duplicate ArchitectureChangedEvent {EventId} ignored by Architecture Engine.", @event.EventId);
            return EventHandlingResult.Ack();
        }

        // 2. Load Playthrough
        var playthrough = await _playthroughRepository.GetByIdAsync(playthroughId, cancellationToken);
        if (playthrough == null)
        {
            return EventHandlingResult.Nack("Playthrough state missing.");
        }

        // 3. Validate Proposed Mutation
        var validationResult = ValidateMutation(playthrough, @event);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Architecture validation failed: {ErrorMessage}", validationResult.ErrorMessage);
            // Rollback: abort without mutating
            return EventHandlingResult.Nack($"Validation failed: {validationResult.ErrorMessage}");
        }

        // 4. Mutate Aggregate
        ApplyMutation(playthrough, @event);

        // 5. Save Aggregate
        await _playthroughRepository.SaveAsync(playthrough, cancellationToken);

        // 6. Publish Events (Strict deterministic order)
        // Order: Nodes Added -> Edges Added -> Edges Removed -> Nodes Removed
        await PublishConsequenceEventsAsync(@event, cancellationToken);

        return EventHandlingResult.Ack();
    }

    private ArchitectureValidationResult ValidateMutation(ArchitectsJourney.Domain.Aggregates.Playthrough playthrough, ArchitectureChangedEvent @event)
    {
        var parts = @event.TargetId.Split('|');

        switch (@event.MutationType)
        {
            case "AddNode":
                if (parts.Length < 3) return ArchitectureValidationResult.Failure("Invalid TargetId for AddNode. Expected: NodeId|NodeType|Label");
                var nodeToAdd = new ArchitectureNode(parts[0], parts[1], parts[2], parts.Length > 3 ? parts[3] : null);
                return _architectureValidator.ValidateNodeAddition(playthrough.Nodes, playthrough.Edges, nodeToAdd);

            case "RemoveNode":
                if (parts.Length < 1) return ArchitectureValidationResult.Failure("Invalid TargetId for RemoveNode.");
                return _architectureValidator.ValidateNodeRemoval(playthrough.Nodes, playthrough.Edges, parts[0]);

            case "AddEdge":
                if (parts.Length < 4) return ArchitectureValidationResult.Failure("Invalid TargetId for AddEdge. Expected: Source|Target|EdgeType|CommunicationType");
                if (!Enum.TryParse<EdgeType>(parts[2], true, out var edgeType) || !Enum.TryParse<CommunicationType>(parts[3], true, out var commType))
                    return ArchitectureValidationResult.Failure("Invalid EdgeType or CommunicationType");
                var edgeToAdd = new ArchitectureEdge(parts[0], parts[1], edgeType, commType);
                return _architectureValidator.ValidateEdgeAddition(playthrough.Nodes, playthrough.Edges, edgeToAdd);

            case "RemoveEdge":
                if (parts.Length < 3) return ArchitectureValidationResult.Failure("Invalid TargetId for RemoveEdge. Expected: Source|Target|EdgeType");
                return _architectureValidator.ValidateEdgeRemoval(playthrough.Nodes, playthrough.Edges, parts[0], parts[1], parts[2]);

            case "UpdateTechnology":
                if (parts.Length < 2) return ArchitectureValidationResult.Failure("Invalid TargetId for UpdateTechnology. Expected: NodeId|TechnologyId");
                return ArchitectureValidationResult.Success();

            default:
                return ArchitectureValidationResult.Failure($"Unknown mutation type: {@event.MutationType}");
        }
    }

    private static void ApplyMutation(ArchitectsJourney.Domain.Aggregates.Playthrough playthrough, ArchitectureChangedEvent @event)
    {
        var parts = @event.TargetId.Split('|');

        switch (@event.MutationType)
        {
            case "AddNode":
                var nodeToAdd = new ArchitectureNode(parts[0], parts[1], parts[2], parts.Length > 3 ? parts[3] : null);
                playthrough.AddNode(nodeToAdd);
                break;
            case "RemoveNode":
                playthrough.RemoveNode(parts[0]);
                break;
            case "AddEdge":
                var edgeToAdd = new ArchitectureEdge(parts[0], parts[1], Enum.Parse<EdgeType>(parts[2], true), Enum.Parse<CommunicationType>(parts[3], true));
                playthrough.AddEdge(edgeToAdd);
                break;
            case "RemoveEdge":
                playthrough.RemoveEdge(parts[0], parts[1], Enum.Parse<EdgeType>(parts[2], true));
                break;
            case "UpdateTechnology":
                playthrough.UpdateNodeTechnology(parts[0], parts[1]);
                break;
        }
    }

    private async Task PublishConsequenceEventsAsync(ArchitectureChangedEvent @event, CancellationToken cancellationToken)
    {
        var parts = @event.TargetId.Split('|');

        // Deterministic order logic applies when a single event causes multiple changes.
        // Currently MutationType handles single changes. 
        // We evaluate and emit the direct outcome of the mutation.

        switch (@event.MutationType)
        {
            case "AddNode":
                await _eventBus.PublishAsync(new ArchitectureNodeAddedEvent
                {
                    SessionId = @event.SessionId,
                    PlaythroughId = @event.PlaythroughId,
                    MissionId = @event.MissionId,
                    CorrelationId = @event.CorrelationId,
                    CausationId = @event.EventId,
                    NodeId = parts[0],
                    NodeType = parts[1]
                }, cancellationToken);
                break;

            case "AddEdge":
                await _eventBus.PublishAsync(new ArchitectureEdgeAddedEvent
                {
                    SessionId = @event.SessionId,
                    PlaythroughId = @event.PlaythroughId,
                    MissionId = @event.MissionId,
                    CorrelationId = @event.CorrelationId,
                    CausationId = @event.EventId,
                    SourceNodeId = parts[0],
                    TargetNodeId = parts[1],
                    EdgeType = parts[2]
                }, cancellationToken);
                break;

            case "RemoveEdge":
                await _eventBus.PublishAsync(new ArchitectureEdgeRemovedEvent
                {
                    SessionId = @event.SessionId,
                    PlaythroughId = @event.PlaythroughId,
                    MissionId = @event.MissionId,
                    CorrelationId = @event.CorrelationId,
                    CausationId = @event.EventId,
                    SourceNodeId = parts[0],
                    TargetNodeId = parts[1],
                    EdgeType = parts[2]
                }, cancellationToken);
                break;

            case "RemoveNode":
                await _eventBus.PublishAsync(new ArchitectureNodeRemovedEvent
                {
                    SessionId = @event.SessionId,
                    PlaythroughId = @event.PlaythroughId,
                    MissionId = @event.MissionId,
                    CorrelationId = @event.CorrelationId,
                    CausationId = @event.EventId,
                    NodeId = parts[0]
                }, cancellationToken);
                break;
        }
    }

    public Task ReleaseAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
