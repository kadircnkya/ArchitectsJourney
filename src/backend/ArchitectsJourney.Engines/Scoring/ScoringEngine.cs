using ArchitectsJourney.Application.Common;
using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Application.Events;
using ArchitectsJourney.Application.Events.System;
using ArchitectsJourney.Application.Models;
using ArchitectsJourney.Domain.Aggregates;
using Microsoft.Extensions.Logging;

namespace ArchitectsJourney.Engines.Scoring;

public class ScoringEngine : IGameSubsystem
{
    public string SubsystemId => "SCORING_ENGINE";

    private readonly ILogger<ScoringEngine> _logger;
    private readonly IEventBus _eventBus;
    private readonly IPlaythroughRepository _playthroughRepo;
    private readonly IScoreCalculator _scoreCalculator;
    private readonly IEventIdempotencyTracker _idempotencyTracker;
    private readonly EvaluationOptions _options;

    public ScoringEngine(
        ILogger<ScoringEngine> logger,
        IEventBus eventBus,
        IPlaythroughRepository playthroughRepo,
        IScoreCalculator scoreCalculator,
        IEventIdempotencyTracker idempotencyTracker,
        EvaluationOptions options)
    {
        _logger = logger;
        _eventBus = eventBus;
        _playthroughRepo = playthroughRepo;
        _scoreCalculator = scoreCalculator;
        _idempotencyTracker = idempotencyTracker;
        _options = options;
    }

    public Task<SubsystemInitResult> InitializeAsync(SessionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        _logger.LogInformation("ScoringEngine initialized for session {SessionId}", context.SessionId);
        return Task.FromResult(SubsystemInitResult.Ok());
    }

    public Task ReleaseAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task<EventHandlingResult> OnEventAsync(DomainEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (@event is not MissionCompletedEvent missionCompletedEvent)
        {
            return EventHandlingResult.Ack();
        }

        if (!missionCompletedEvent.PlaythroughId.HasValue)
        {
            return EventHandlingResult.Ack();
        }

        if (!await _idempotencyTracker.TryMarkProcessedAsync(missionCompletedEvent.PlaythroughId.Value, @event.EventId, cancellationToken))
        {
            _logger.LogInformation("Event {EventId} already processed by {SubsystemId}", @event.EventId, SubsystemId);
            return EventHandlingResult.Ack();
        }

        try
        {
            var playthrough = await _playthroughRepo.GetByIdAsync(missionCompletedEvent.PlaythroughId.Value, cancellationToken);
            if (playthrough == null)
            {
                _logger.LogWarning("Playthrough {PlaythroughId} not found", missionCompletedEvent.PlaythroughId);
                return EventHandlingResult.Ack(); // Cannot evaluate without playthrough
            }

            if (playthrough.EvaluationCompleted)
            {
                _logger.LogInformation("Playthrough {PlaythroughId} already evaluated", playthrough.Id);
                return EventHandlingResult.Ack();
            }

            // 1. Calculate Score (pure)
            var evaluationResult = _scoreCalculator.Calculate(playthrough, _options);

            // 2. Mutate Aggregate
            playthrough.UpdateScore(evaluationResult.TotalScore);
            playthrough.CompleteEvaluation(evaluationResult.Rank, evaluationResult.MissionResult, DateTimeOffset.UtcNow);

            // 3. Save Aggregate
            await _playthroughRepo.SaveAsync(playthrough, cancellationToken);

            // 4. Publish Events (Strict ordering: Score -> Rank -> Evaluation Completed)
            var scoreEvent = new ScoreCalculatedEvent
            {
                SessionId = missionCompletedEvent.SessionId,
                PlaythroughId = playthrough.Id,
                MissionId = playthrough.MissionId,
                CorrelationId = missionCompletedEvent.CorrelationId,
                CausationId = missionCompletedEvent.EventId,
                TotalScore = evaluationResult.TotalScore
            };
            await _eventBus.PublishAsync(scoreEvent, cancellationToken);

            var rankEvent = new RankAssignedEvent
            {
                SessionId = missionCompletedEvent.SessionId,
                PlaythroughId = playthrough.Id,
                MissionId = playthrough.MissionId,
                CorrelationId = missionCompletedEvent.CorrelationId,
                CausationId = missionCompletedEvent.EventId,
                Rank = evaluationResult.Rank.ToString()
            };
            await _eventBus.PublishAsync(rankEvent, cancellationToken);

            var completedEvent = new MissionEvaluationCompletedEvent
            {
                SessionId = missionCompletedEvent.SessionId,
                PlaythroughId = playthrough.Id,
                MissionId = playthrough.MissionId,
                CorrelationId = missionCompletedEvent.CorrelationId,
                CausationId = missionCompletedEvent.EventId,
                MissionResult = evaluationResult.MissionResult.ToString()
            };
            await _eventBus.PublishAsync(completedEvent, cancellationToken);

            return EventHandlingResult.Ack();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing event {EventId} in {SubsystemId}", @event.EventId, SubsystemId);
            return EventHandlingResult.Nack(ex.Message);
        }
    }
}
