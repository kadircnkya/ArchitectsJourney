using ArchitectsJourney.Application.Common;
using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Application.Events;
using ArchitectsJourney.Application.Events.Metric;
using ArchitectsJourney.Application.Events.Rule;
using ArchitectsJourney.Domain.Aggregates;
using ArchitectsJourney.Domain.Enums;
using ArchitectsJourney.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ArchitectsJourney.Engines.Metric;

/// <summary>
/// Metric evaluation subsystem updating numeric aggregates.
/// Defined in Document 05.
/// </summary>
public sealed class MetricEngine : IGameSubsystem
{
    private readonly ILogger<MetricEngine> _logger;
    private readonly IEventBus _eventBus;
    private readonly IPlaythroughRepository _playthroughRepository;
    private readonly IMissionRepository _missionRepository;
    private readonly IMetricConfiguration _metricConfiguration;
    private readonly IEventIdempotencyTracker _eventIdempotencyTracker;

    public string SubsystemId => "METRIC_ENGINE";

    public MetricEngine(
        ILogger<MetricEngine> logger,
        IEventBus eventBus,
        IPlaythroughRepository playthroughRepository,
        IMissionRepository missionRepository,
        IMetricConfiguration metricConfiguration,
        IEventIdempotencyTracker eventIdempotencyTracker)
    {
        _logger = logger;
        _eventBus = eventBus;
        _playthroughRepository = playthroughRepository;
        _missionRepository = missionRepository;
        _metricConfiguration = metricConfiguration;
        _eventIdempotencyTracker = eventIdempotencyTracker;
    }

    public Task<SubsystemInitResult> InitializeAsync(SessionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        _logger.LogInformation("Metric Engine initialized for playthrough {PlaythroughId}", context.PlaythroughId);
        return Task.FromResult(SubsystemInitResult.Ok());
    }

    public async Task<EventHandlingResult> OnEventAsync(DomainEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (!@event.PlaythroughId.HasValue)
        {
            return EventHandlingResult.Nack("PlaythroughId missing.");
        }

        try
        {
            if (@event is DecisionProcessedEvent decisionEvent)
            {
                return await HandleDecisionProcessedAsync(decisionEvent, @event.PlaythroughId.Value, cancellationToken);
            }
            else if (@event is MetricDeltaAppliedEvent metricEvent)
            {
                return await HandleMetricDeltaAppliedAsync(metricEvent, @event.PlaythroughId.Value, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Metric Engine execution failed.");
            return EventHandlingResult.Nack("Metric Engine error: " + ex.Message);
        }

        return EventHandlingResult.Ack();
    }

    private async Task<EventHandlingResult> HandleDecisionProcessedAsync(DecisionProcessedEvent @event, Guid playthroughId, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[MetricEngine] Received DecisionProcessedEvent for Option {@event.OptionId}");
        // Check idempotency: Have we orchestrated metric impacts for this decision event?
        if (await _eventIdempotencyTracker.TryMarkProcessedAsync(playthroughId, @event.EventId, cancellationToken))
        {
            if (string.IsNullOrEmpty(@event.MissionId)) return EventHandlingResult.Nack("MissionId is missing.");
            var mission = await _missionRepository.GetByIdAsync(@event.MissionId, cancellationToken);
            if (mission == null) return EventHandlingResult.Nack("Mission not found.");

            var decisionPoint = mission.DecisionPoints.FirstOrDefault(dp => dp.Id == @event.DecisionPointId);
            var option = decisionPoint?.Options.FirstOrDefault(opt => opt.Id == @event.OptionId);

            if (option != null && option.MetricImpacts.Count > 0)
            {
                // Orchestrate: Publish MetricDeltaAppliedEvent for each option impact
                foreach (var impact in option.MetricImpacts)
                {
                    Console.WriteLine($"[MetricEngine] Found impact for {impact.Metric}: {impact.Value}");
                    var metricDeltaAppliedEvent = new MetricDeltaAppliedEvent
                    {
                        SessionId = @event.SessionId,
                        PlaythroughId = @event.PlaythroughId,
                        MissionId = @event.MissionId,
                        CorrelationId = @event.CorrelationId,
                        CausationId = @event.EventId,
                        Metric = impact.Metric.ToString(),
                        Value = impact.Value
                    };
                    await _eventBus.PublishAsync(metricDeltaAppliedEvent, cancellationToken);
                    _logger.LogInformation("Orchestrated option impact: published MetricDeltaAppliedEvent for {Metric} -> {Value}", impact.Metric, impact.Value);
                }
            }
        }
        else
        {
            _logger.LogWarning("Duplicate DecisionProcessedEvent {EventId} ignored by Metric Engine.", @event.EventId);
        }

        return EventHandlingResult.Ack();
    }

    private async Task<EventHandlingResult> HandleMetricDeltaAppliedAsync(MetricDeltaAppliedEvent @event, Guid playthroughId, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[MetricEngine] Received MetricDeltaAppliedEvent for {@event.Metric} = {@event.Value}");
        // Guarantee exactly-once metric mutation
        if (!await _eventIdempotencyTracker.TryMarkProcessedAsync(playthroughId, @event.EventId, cancellationToken))
        {
            _logger.LogWarning("Duplicate MetricDeltaAppliedEvent {EventId} ignored by Metric Engine.", @event.EventId);
            return EventHandlingResult.Ack();
        }

        var playthrough = await _playthroughRepository.GetByIdAsync(playthroughId, cancellationToken);
        if (playthrough == null) return EventHandlingResult.Nack("Playthrough state missing.");

        if (Enum.TryParse<MetricType>(@event.Metric, true, out var metricType))
        {
            playthrough.Metrics.TryGetValue(metricType, out var oldValue);
            int newValue = oldValue + @event.Value;

            // Enforce Bounds
            int enforcedValue = Math.Clamp(newValue, 0, 100);

            if (enforcedValue != newValue)
            {
                var boundsEvent = new MetricBoundsEnforcedEvent
                {
                    SessionId = @event.SessionId,
                    PlaythroughId = @event.PlaythroughId,
                    MissionId = @event.MissionId,
                    CorrelationId = @event.CorrelationId,
                    CausationId = @event.EventId,
                    Metric = @event.Metric,
                    AttemptedValue = newValue,
                    EnforcedValue = enforcedValue
                };
                await _eventBus.PublishAsync(boundsEvent, cancellationToken);
                _logger.LogInformation("Metric {Metric} bounded. Attempted {Attempted}, Enforced {Enforced}", @event.Metric, newValue, enforcedValue);
            }

            playthrough.SetMetricValue(metricType, enforcedValue);

            // Check Thresholds
            foreach (var threshold in _metricConfiguration.Thresholds)
            {
                bool crossedUp = oldValue < threshold && enforcedValue >= threshold;
                bool crossedDown = oldValue > threshold && enforcedValue <= threshold;

                if (crossedUp || crossedDown)
                {
                    var thresholdEvent = new MetricThresholdCrossedEvent
                    {
                        SessionId = @event.SessionId,
                        PlaythroughId = @event.PlaythroughId,
                        MissionId = @event.MissionId,
                        CorrelationId = @event.CorrelationId,
                        CausationId = @event.EventId,
                        Metric = @event.Metric,
                        ThresholdValue = threshold,
                        CrossedUpwards = crossedUp
                    };
                    await _eventBus.PublishAsync(thresholdEvent, cancellationToken);
                    _logger.LogInformation("Metric {Metric} crossed threshold {Threshold}. Old: {Old}, New: {New}", @event.Metric, threshold, oldValue, enforcedValue);
                }
            }

            // Append to Metric History
            var historyEntry = new MetricHistoryEntry
            {
                CausationId = @event.CausationId ?? @event.EventId,
                Timestamp = DateTimeOffset.UtcNow,
                Metrics = playthrough.Metrics.ToDictionary(k => k.Key.ToString(), v => v.Value)
            };
            playthrough.AppendMetricHistory(historyEntry);

            await _playthroughRepository.SaveAsync(playthrough, cancellationToken);
        }

        return EventHandlingResult.Ack();
    }

    public Task ReleaseAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
