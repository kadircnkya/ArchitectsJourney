using ArchitectsJourney.Application.Common;
using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Application.Events;
using ArchitectsJourney.Application.Events.Architecture;
using ArchitectsJourney.Application.Events.System;
using ArchitectsJourney.Application.Events.Technology;
using ArchitectsJourney.Application.Models;
using ArchitectsJourney.Domain.Aggregates;
using Microsoft.Extensions.Logging;

namespace ArchitectsJourney.Engines.Achievements;

public class AchievementEngine : IGameSubsystem
{
    public string SubsystemId => "ACHIEVEMENT_ENGINE";

    private readonly ILogger<AchievementEngine> _logger;
    private readonly IEventBus _eventBus;
    private readonly IPlaythroughRepository _playthroughRepo;
    private readonly IAchievementEvaluator _evaluator;
    private readonly IEventIdempotencyTracker _tracker;
    private readonly AchievementOptions _options;

    public AchievementEngine(
        ILogger<AchievementEngine> logger,
        IEventBus eventBus,
        IPlaythroughRepository playthroughRepo,
        IAchievementEvaluator evaluator,
        IEventIdempotencyTracker tracker,
        AchievementOptions options)
    {
        _logger = logger;
        _eventBus = eventBus;
        _playthroughRepo = playthroughRepo;
        _evaluator = evaluator;
        _tracker = tracker;
        _options = options;
    }

    public Task<SubsystemInitResult> InitializeAsync(SessionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        _logger.LogInformation("AchievementEngine initialized for session {SessionId}", context.SessionId);
        return Task.FromResult(SubsystemInitResult.Ok());
    }

    public Task ReleaseAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task<EventHandlingResult> OnEventAsync(DomainEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        bool isRelevant = @event is MissionEvaluationCompletedEvent ||
                          @event is MissionObjectiveCompletedEvent ||
                          @event is TechnologyUnlockedEvent ||
                          @event is ArchitectureNodeAddedEvent ||
                          @event is RankAssignedEvent;

        if (!isRelevant)
        {
            return EventHandlingResult.Ack();
        }

        Guid? playthroughId = null;
        Guid? sessionId = null;

        // Try extracting properties dynamically or by checking concrete types. 
        // We know these events have PlaythroughId and SessionId.
        if (@event is MissionEvaluationCompletedEvent m) { playthroughId = m.PlaythroughId; sessionId = m.SessionId; }
        else if (@event is MissionObjectiveCompletedEvent o) { playthroughId = o.PlaythroughId; sessionId = o.SessionId; }
        else if (@event is TechnologyUnlockedEvent t) { playthroughId = t.PlaythroughId; sessionId = t.SessionId; }
        else if (@event is ArchitectureNodeAddedEvent a) { playthroughId = a.PlaythroughId; sessionId = a.SessionId; }
        else if (@event is RankAssignedEvent r) { playthroughId = r.PlaythroughId; sessionId = r.SessionId; }

        if (playthroughId == null || sessionId == null)
        {
            return EventHandlingResult.Ack();
        }

        if (!await _tracker.TryMarkProcessedAsync(playthroughId.Value, @event.EventId, cancellationToken))
        {
            _logger.LogInformation("Event {EventId} already processed by {SubsystemId}", @event.EventId, SubsystemId);
            return EventHandlingResult.Ack();
        }

        try
        {
            var playthrough = await _playthroughRepo.GetByIdAsync(playthroughId.Value, cancellationToken);
            if (playthrough == null)
            {
                return EventHandlingResult.Ack();
            }

            var result = _evaluator.Evaluate(playthrough, _options);

            if (result.UnlockedAchievements.Count == 0 && result.AwardedExperience == 0 && result.NewLevel == playthrough.PlayerLevel)
            {
                return EventHandlingResult.Ack();
            }

            // Mutate aggregate
            foreach (var ach in result.UnlockedAchievements)
            {
                playthrough.UnlockAchievement(ach);
            }
            if (result.AwardedExperience > 0)
            {
                playthrough.AddExperience(result.AwardedExperience);
            }
            
            int oldLevel = playthrough.PlayerLevel;
            if (result.NewLevel > oldLevel)
            {
                playthrough.UpdatePlayerLevel(result.NewLevel);
            }

            // Save aggregate
            await _playthroughRepo.SaveAsync(playthrough, cancellationToken);

            // Publish events
            foreach (var ach in result.UnlockedAchievements)
            {
                var ev = new AchievementUnlockedEvent
                {
                    SessionId = sessionId.Value,
                    PlaythroughId = playthrough.Id,
                    MissionId = playthrough.MissionId,
                    CorrelationId = @event.CorrelationId,
                    CausationId = @event.EventId,
                    AchievementId = ach
                };
                await _eventBus.PublishAsync(ev, cancellationToken);
            }

            if (result.AwardedExperience > 0)
            {
                var ev = new ExperienceAwardedEvent
                {
                    SessionId = sessionId.Value,
                    PlaythroughId = playthrough.Id,
                    MissionId = playthrough.MissionId,
                    CorrelationId = @event.CorrelationId,
                    CausationId = @event.EventId,
                    Amount = result.AwardedExperience,
                    Reason = "Achievements Unlocked"
                };
                await _eventBus.PublishAsync(ev, cancellationToken);
            }

            if (result.NewLevel > oldLevel)
            {
                var ev = new PlayerLevelChangedEvent
                {
                    SessionId = sessionId.Value,
                    PlaythroughId = playthrough.Id,
                    MissionId = playthrough.MissionId,
                    CorrelationId = @event.CorrelationId,
                    CausationId = @event.EventId,
                    OldLevel = oldLevel,
                    NewLevel = result.NewLevel
                };
                await _eventBus.PublishAsync(ev, cancellationToken);
            }

            return EventHandlingResult.Ack();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event {EventId} in {SubsystemId}", @event.EventId, SubsystemId);
            return EventHandlingResult.Nack(ex.Message);
        }
    }
}
