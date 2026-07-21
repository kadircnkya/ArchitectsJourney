using ArchitectsJourney.Application.Common;
using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Application.Events;
using ArchitectsJourney.Application.Events.Architecture;
using ArchitectsJourney.Application.Events.Rule;
using ArchitectsJourney.Application.Events.System;
using ArchitectsJourney.Domain.Aggregates;
using ArchitectsJourney.Domain.Entities;
using ArchitectsJourney.Domain.Enums;
using ArchitectsJourney.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ArchitectsJourney.Engines.Mission;

public sealed class MissionEngine : IGameSubsystem
{
    private readonly ILogger<MissionEngine> _logger;
    private readonly IEventBus _eventBus;
    private readonly IPlaythroughRepository _playthroughRepository;
    private readonly IMissionLoader _missionLoader;
    private readonly IEventIdempotencyTracker _idempotencyTracker;

    public string SubsystemId => "MISSION_ENGINE";

    public MissionEngine(
        ILogger<MissionEngine> logger,
        IEventBus eventBus,
        IPlaythroughRepository playthroughRepository,
        IMissionLoader missionLoader,
        IEventIdempotencyTracker idempotencyTracker)
    {
        _logger = logger;
        _eventBus = eventBus;
        _playthroughRepository = playthroughRepository;
        _missionLoader = missionLoader;
        _idempotencyTracker = idempotencyTracker;
    }

    public Task<SubsystemInitResult> InitializeAsync(SessionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        _logger.LogInformation("MissionEngine initialized for {SessionId}", context.SessionId);
        return Task.FromResult(SubsystemInitResult.Ok());
    }

    public async Task<EventHandlingResult> OnEventAsync(DomainEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (!@event.PlaythroughId.HasValue || string.IsNullOrEmpty(@event.MissionId))
            return EventHandlingResult.Ack();

        // Ensure we only process supported events
        if (@event.EventType != EventTypes.Rule.DecisionProcessed &&
            @event.EventType != EventTypes.Metric.Updated &&
            @event.EventType != EventTypes.Architecture.Changed)
        {
            return EventHandlingResult.Ack();
        }

        try
        {
            if (!await _idempotencyTracker.TryMarkProcessedAsync(@event.PlaythroughId.Value, @event.EventId, cancellationToken))
            {
                _logger.LogInformation("Duplicate mission event {EventId} ignored.", @event.EventId);
                return EventHandlingResult.Ack();
            }

            var playthrough = await _playthroughRepository.GetByIdAsync(@event.PlaythroughId.Value, cancellationToken);
            if (playthrough == null) return EventHandlingResult.Nack("Playthrough not found.");

            var mission = await _missionLoader.LoadMissionAsync(@event.MissionId, cancellationToken);

            // If the playthrough has no objectives initialized, initialize them
            if (playthrough.Objectives.Count == 0 && mission.Objectives.Count > 0)
            {
                playthrough.InitializeObjectives(mission.Objectives.Select(o => o.Id));
            }

            var newlyCompleted = new List<string>();
            var newlyFailed = new List<string>();

            // Read-only evaluation
            foreach (var objDef in mission.Objectives)
            {
                var state = playthrough.Objectives.FirstOrDefault(o => o.Id == objDef.Id);
                if (state == null || state.State != ObjectiveState.Pending)
                    continue; // Already processed

                var isCompleted = true;
                foreach (var condition in objDef.Conditions)
                {
                    if (!EvaluateCondition(condition, playthrough))
                    {
                        isCompleted = false;
                        break;
                    }
                }

                if (isCompleted)
                {
                    newlyCompleted.Add(objDef.Id);
                }
            }

            // If nothing changed, we do no mutation, no persistence, no events.
            if (newlyCompleted.Count == 0 && newlyFailed.Count == 0)
            {
                return EventHandlingResult.Ack();
            }

            // Mutate Aggregate
            foreach (var id in newlyCompleted)
            {
                playthrough.CompleteObjective(id);
            }
            foreach (var id in newlyFailed)
            {
                playthrough.FailObjective(id);
            }

            // Save Aggregate
            await _playthroughRepository.SaveAsync(playthrough, cancellationToken);

            // Publish Events only after successful persistence
            // Deterministic publish order:
            // 1. MissionObjectiveCompletedEvent
            // 2. MissionObjectiveFailedEvent
            // 3. System.MissionCompleted (if all objectives are complete)

            foreach (var id in newlyCompleted)
            {
                var evt = new MissionObjectiveCompletedEvent
                {
                    SessionId = @event.SessionId,
                    PlaythroughId = @event.PlaythroughId,
                    MissionId = @event.MissionId,
                    CorrelationId = @event.CorrelationId,
                    CausationId = @event.EventId,
                    ObjectiveId = id
                };
                await _eventBus.PublishAsync(evt, cancellationToken);
            }

            foreach (var id in newlyFailed)
            {
                var evt = new MissionObjectiveFailedEvent
                {
                    SessionId = @event.SessionId,
                    PlaythroughId = @event.PlaythroughId,
                    MissionId = @event.MissionId,
                    CorrelationId = @event.CorrelationId,
                    CausationId = @event.EventId,
                    ObjectiveId = id
                };
                await _eventBus.PublishAsync(evt, cancellationToken);
            }

            if (playthrough.Objectives.Count > 0 && playthrough.Objectives.All(o => o.State == ObjectiveState.Completed))
            {
                var evt = new MissionCompletedEvent
                {
                    SessionId = @event.SessionId,
                    PlaythroughId = @event.PlaythroughId,
                    MissionId = @event.MissionId,
                    CorrelationId = @event.CorrelationId,
                    CausationId = @event.EventId
                };
                await _eventBus.PublishAsync(evt, cancellationToken);
            }

            return EventHandlingResult.Ack();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MissionEngine evaluation failed.");
            return EventHandlingResult.Nack(ex.Message);
        }
    }

    private static bool EvaluateCondition(ObjectiveCondition condition, Playthrough playthrough)
    {
        if (condition.Type.Equals("Metric", StringComparison.OrdinalIgnoreCase))
        {
            if (Enum.TryParse<MetricType>(condition.Target, true, out var metricType))
            {
                if (playthrough.Metrics.TryGetValue(metricType, out var currentValue))
                {
                    if (!int.TryParse(condition.Value, out var targetValue))
                        return false;

                    return condition.Operator switch
                    {
                        ">" => currentValue > targetValue,
                        ">=" => currentValue >= targetValue,
                        "<" => currentValue < targetValue,
                        "<=" => currentValue <= targetValue,
                        "==" => currentValue == targetValue,
                        _ => false
                    };
                }
            }
            return false; // Metric not found
        }
        else if (condition.Type.Equals("ArchitectureNode", StringComparison.OrdinalIgnoreCase))
        {
            var nodeExists = playthrough.Nodes.Any(n => 
                n.Type.Equals(condition.Target, StringComparison.OrdinalIgnoreCase) || 
                n.Label.Equals(condition.Target, StringComparison.OrdinalIgnoreCase));
            
            if (bool.TryParse(condition.Value, out var targetExists))
            {
                return nodeExists == targetExists;
            }
            return false;
        }

        return false;
    }

    public Task ReleaseAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
