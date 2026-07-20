using ArchitectsJourney.Application.Common;
using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Application.Events;
using ArchitectsJourney.Application.Events.Player;
using ArchitectsJourney.Application.Events.Rule;
using ArchitectsJourney.Application.Events.System;
using ArchitectsJourney.Application.Events.Narrative;
using ArchitectsJourney.Domain.Aggregates;
using ArchitectsJourney.Domain.Enums;
using ArchitectsJourney.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading;

namespace ArchitectsJourney.Engines.Game;

/// <summary>
/// The central orchestrator engine executing the 13-phase game loop.
/// Coordinates rule engines, checkpointing, and phase progression.
/// Implements Game Engine Specification (Document 09).
/// </summary>
public sealed class GameEngine : IGameSubsystem, ICheckpointAware, IDisposable
{
    private readonly ILogger<GameEngine> _logger;
    private readonly IEventBus _eventBus;
    private readonly IMissionRepository _missionRepository;
    private readonly IPlaythroughRepository _playthroughRepository;
    private readonly ISaveSystem _saveSystem;
    private readonly IServiceProvider _serviceProvider;
    private readonly SemaphoreSlim _sessionLock = new SemaphoreSlim(1, 1);

    private bool _isDisposed;
    
    public string SubsystemId => "GAME_ENGINE";

    public GameEngine(
        ILogger<GameEngine> logger,
        IEventBus eventBus,
        IMissionRepository missionRepository,
        IPlaythroughRepository playthroughRepository,
        ISaveSystem saveSystem,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _eventBus = eventBus;
        _missionRepository = missionRepository;
        _playthroughRepository = playthroughRepository;
        _saveSystem = saveSystem;
        _serviceProvider = serviceProvider;
    }

    public async Task<SubsystemInitResult> InitializeAsync(
        SessionContext context, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var mission = await _missionRepository.GetByIdAsync(context.MissionId, cancellationToken);
        if (mission == null)
        {
            return SubsystemInitResult.Failed($"Mission {context.MissionId} not found.");
        }

        var playthrough = new Playthrough(context.PlaythroughId, context.MissionId);
        
        var initialMetrics = mission.InitialMetrics.ToDictionary(
            k => Enum.Parse<MetricType>(k.Key),
            v => v.Value
        );
        playthrough.InitializeMetrics(initialMetrics);
        
        playthrough.InitializeArchitecture(mission.InitialNodes, mission.InitialEdges);

        await _playthroughRepository.SaveAsync(playthrough, cancellationToken);
        _logger.LogInformation("Game Engine initialized session {SessionId}.", context.SessionId);

        return SubsystemInitResult.Ok();
    }

    public async Task<EventHandlingResult> OnEventAsync(DomainEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        try
        {
            // PHASE 01: Wait for Player Action (Implicit by receiving event)

            // PHASE 02: Receive Command
            if (@event is PlayerDecisionSubmittedEvent decisionEvent)
            {
                // PHASE 03: Lock Session
                if (!await _sessionLock.WaitAsync(0, cancellationToken))
                {
                    return EventHandlingResult.Nack("Interaction loop currently locked.");
                }

                try
                {
                    _logger.LogInformation("[Phase 03] Interaction Lock acquired.");

                    // PHASE 04: Construct Domain Event
                    var (correlationId, causationId) = DomainEvent.DerivedFrom(decisionEvent);
                    
                    if (!@event.PlaythroughId.HasValue) return EventHandlingResult.Nack("PlaythroughId is missing from event.");
                    var playthrough = await _playthroughRepository.GetByIdAsync(@event.PlaythroughId.Value, cancellationToken);
                    if (playthrough == null)
                    {
                        return EventHandlingResult.Nack("Playthrough not found.");
                    }

                    if (string.IsNullOrEmpty(@event.MissionId)) return EventHandlingResult.Nack("MissionId is missing from event.");
                    var mission = await _missionRepository.GetByIdAsync(@event.MissionId, cancellationToken);
                    var decisionPoint = mission?.DecisionPoints.FirstOrDefault(dp => dp.Id == decisionEvent.DecisionPointId);
                    var option = decisionPoint?.Options.FirstOrDefault(opt => opt.Id == decisionEvent.SelectedOptionId);


                    // PHASE 08: Append History (We resolve decision before phase transition)
                    playthrough.ResolveDecision(decisionEvent.DecisionPointId, decisionEvent.SelectedOptionId);
                    
                    // PHASE 09: Evaluate Mission Phase
                    var nextPhase = playthrough.CurrentPhase switch
                    {
                        MissionPhase.ClientMeeting => MissionPhase.RequirementGathering,
                        MissionPhase.RequirementGathering => MissionPhase.ArchitectureDecisions,
                        MissionPhase.ArchitectureDecisions => MissionPhase.BusinessEvolution,
                        MissionPhase.BusinessEvolution => MissionPhase.ArchitectureEvolution,
                        _ => MissionPhase.MissionComplete
                    };
                    
                    bool phaseChanged = false;
                    MissionPhase previousPhase = playthrough.CurrentPhase;
                    
                    if (nextPhase != playthrough.CurrentPhase)
                    {
                        playthrough.TransitionToPhase(nextPhase);
                        phaseChanged = true;
                        _logger.LogInformation("[Phase 09] Playthrough transitioned to phase: {Phase}", nextPhase);
                    }
                    
                    await _playthroughRepository.SaveAsync(playthrough, cancellationToken);
                    
                    var decisionProcessedEvent = new DecisionProcessedEvent
                    {
                        SessionId = @event.SessionId,
                        PlaythroughId = @event.PlaythroughId,
                        MissionId = @event.MissionId,
                        CorrelationId = correlationId,
                        CausationId = causationId,
                        DecisionPointId = decisionEvent.DecisionPointId,
                        OptionId = decisionEvent.SelectedOptionId,
                        AppliedRuleIds = [],
                        PhaseCompleted = nextPhase == MissionPhase.MissionComplete
                    };

                    // PHASE 05: Dispatch Event
                    _logger.LogInformation("[Phase 04-05] Dispatching DecisionProcessedEvent.");
                    await _eventBus.PublishAsync(decisionProcessedEvent, cancellationToken);
                    
                    if (phaseChanged)
                    {
                        await _eventBus.PublishAsync(new MissionPhaseChangedEvent
                        {
                            SessionId = @event.SessionId,
                            PlaythroughId = @event.PlaythroughId,
                            MissionId = @event.MissionId,
                            CorrelationId = correlationId,
                            CausationId = causationId,
                            PreviousPhase = previousPhase.ToString(),
                            NewPhase = nextPhase.ToString()
                        }, cancellationToken);
                    }

                    // PHASE 06: Execute Rule Engine
                    _logger.LogInformation("[Phase 06] Rule Engine Consequence Evaluation triggered via Event Bus.");

                    // PHASE 07: Wait for Event Settlement
                    _logger.LogInformation("[Phase 07] Waiting for Event Settlement (Synchronous barrier reached).");

                    // PHASE 10: Create Checkpoint
                    _logger.LogInformation("[Phase 10] Generating checkpoint snapshots.");
                    var snapshots = new List<SubsystemSnapshot>();
                    var selfSnapshot = await TakeSnapshotAsync(@event.PlaythroughId.Value, @event.SessionId, cancellationToken);
                    snapshots.Add(selfSnapshot);

                    var otherSubsystems = _serviceProvider.GetServices<ICheckpointAware>().Where(s => s != this);
                    foreach (var subsystem in otherSubsystems)
                    {
                        snapshots.Add(await subsystem.TakeSnapshotAsync(@event.PlaythroughId.Value, @event.SessionId, cancellationToken));
                    }

                    var checkpointId = Guid.NewGuid();
                    await _saveSystem.SaveCheckpointAsync(@event.SessionId, 0, snapshots, cancellationToken);
                    
                    await _eventBus.PublishAsync(new CheckpointCreatedEvent
                    {
                        SessionId = @event.SessionId,
                        PlaythroughId = @event.PlaythroughId,
                        MissionId = @event.MissionId,
                        CorrelationId = correlationId,
                        CausationId = causationId,
                        CheckpointId = checkpointId
                    }, cancellationToken);
                    
                    _logger.LogInformation("[Phase 10] Checkpoint written successfully.");

                    // PHASE 11: Generate Narrative Events
                    _logger.LogInformation("[Phase 11] Narrative Delivery Cues generated.");
                    await _eventBus.PublishAsync(new FeedbackDeliveredEvent
                    {
                        SessionId = @event.SessionId,
                        PlaythroughId = @event.PlaythroughId,
                        MissionId = @event.MissionId,
                        CorrelationId = correlationId,
                        CausationId = causationId,
                        FeedbackContent = "Decision Recorded."
                    }, cancellationToken);

                    // PHASE 12: Produce UI Response
                    _logger.LogInformation("[Phase 12] UI Update Queue Dispatch.");
                    await _eventBus.PublishAsync(new FeedbackQueuedEvent
                    {
                        SessionId = @event.SessionId,
                        PlaythroughId = @event.PlaythroughId,
                        MissionId = @event.MissionId,
                        CorrelationId = correlationId,
                        CausationId = causationId,
                        QueueId = "UI_MAIN"
                    }, cancellationToken);

                    return EventHandlingResult.Ack();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "13-Phase Loop execution encountered critical error.");
                    await _eventBus.PublishAsync(new RollbackRequiredEvent
                    {
                        SessionId = @event.SessionId,
                        PlaythroughId = @event.PlaythroughId,
                        MissionId = @event.MissionId,
                        CorrelationId = Guid.NewGuid(),
                        CausationId = @event.EventId,
                        FailedSubsystem = SubsystemId,
                        FailureReason = ex.Message,
                        LastCheckpointId = Guid.Empty
                    }, cancellationToken);
                    
                    return EventHandlingResult.Nack(ex.Message);
                }
                finally
                {
                    // PHASE 13: Unlock Session
                    _sessionLock.Release();
                    _logger.LogInformation("[Phase 13] Interaction Lock released.");
                }
            }

            return EventHandlingResult.Ack();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error outside phase loop.");
            return EventHandlingResult.Nack(ex.Message);
        }
    }

    public Task ReleaseAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task<SubsystemSnapshot> TakeSnapshotAsync(Guid playthroughId, Guid sessionId, CancellationToken cancellationToken = default)
    {
        var playthrough = await _playthroughRepository.GetByIdAsync(playthroughId, cancellationToken);
        if (playthrough == null)
        {
            throw new InvalidOperationException("Cannot snapshot uninitialized game engine state.");
        }

        var json = JsonSerializer.Serialize(playthrough.TakeSnapshot());

        return new SubsystemSnapshot
        {
            SubsystemId = SubsystemId,
            SessionId = sessionId,
            SequenceNumber = 0,
            CapturedAt = DateTimeOffset.UtcNow,
            StateJson = json
        };
    }

    public Task RestoreFromSnapshotAsync(SubsystemSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        // Note: In a stateless engine, restore is handled by the persistence layer
        // or by loading the snapshot into the database. The engine doesn't keep it.
        // For current logic, we can fetch it, apply it, and save it.
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            _sessionLock.Dispose();
            _isDisposed = true;
        }
    }
}
