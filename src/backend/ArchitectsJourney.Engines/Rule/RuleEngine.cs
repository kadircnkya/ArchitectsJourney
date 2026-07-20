using ArchitectsJourney.Application.Common;
using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Application.Events;
using ArchitectsJourney.Application.Events.Architecture;
using ArchitectsJourney.Application.Events.Metric;
using ArchitectsJourney.Application.Events.Rule;
using ArchitectsJourney.Application.Events.Technology;
using ArchitectsJourney.Domain.Aggregates;
using ArchitectsJourney.Engines.Rule.Parsing;
using ArchitectsJourney.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ArchitectsJourney.Engines.Rule;

/// <summary>
/// Evaluates business rules and produces gameplay consequences downstream.
/// Implements Rule Engine Specification (Document 07).
/// </summary>
public sealed class RuleEngine : IGameSubsystem
{
    private readonly ILogger<RuleEngine> _logger;
    private readonly IEventBus _eventBus;
    private readonly IMissionRepository _missionRepository;
    private readonly IPlaythroughRepository _playthroughRepository;
    private readonly IConditionEvaluator _conditionEvaluator;
    private readonly IEffectParser _effectParser;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, HashSet<string>> _correlationExecutedRules = new();

    public string SubsystemId => "RULE_ENGINE";

    public RuleEngine(
        ILogger<RuleEngine> logger,
        IEventBus eventBus,
        IMissionRepository missionRepository,
        IPlaythroughRepository playthroughRepository,
        IConditionEvaluator conditionEvaluator,
        IEffectParser effectParser)
    {
        _logger = logger;
        _eventBus = eventBus;
        _missionRepository = missionRepository;
        _playthroughRepository = playthroughRepository;
        _conditionEvaluator = conditionEvaluator;
        _effectParser = effectParser;
    }

    public async Task<SubsystemInitResult> InitializeAsync(
        SessionContext context, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        var mission = await _missionRepository.GetByIdAsync(context.MissionId, cancellationToken);
        if (mission == null)
        {
            return SubsystemInitResult.Failed($"Mission content {context.MissionId} failed to load for evaluation context.");
        }

        _logger.LogInformation("Rule Engine initialized containing {Count} evaluation rules.", mission.Rules.Count);
        return SubsystemInitResult.Ok();
    }

    public async Task<EventHandlingResult> OnEventAsync(DomainEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (string.IsNullOrEmpty(@event.MissionId)) return EventHandlingResult.Nack("MissionId is missing.");
        var mission = await _missionRepository.GetByIdAsync(@event.MissionId, cancellationToken);
        if (mission == null)
        {
            return EventHandlingResult.Nack("Rule Engine evaluation contexts not loaded.");
        }

        // Stage 1: Event Reception
        string triggerName = MapEventToTrigger(@event);
        if (string.IsNullOrEmpty(triggerName))
        {
            return EventHandlingResult.Ack();
        }

        try
        {
            if (!@event.PlaythroughId.HasValue) return EventHandlingResult.Nack("PlaythroughId missing.");
            var playthrough = await _playthroughRepository.GetByIdAsync(@event.PlaythroughId.Value, cancellationToken);
            if (playthrough == null) return EventHandlingResult.Nack("Playthrough state missing.");

            // Stage 2: Rule Identification
            var candidateRules = mission.Rules.Where(r => r.Trigger == triggerName).ToList();
            if (candidateRules.Count == 0) return EventHandlingResult.Ack();

            var executedRulesInContext = _correlationExecutedRules.GetOrAdd(@event.CorrelationId, _ => new HashSet<string>());

            // Stage 3: Condition Evaluation
            var activeRules = new List<MissionRuleDefinition>();
            foreach (var rule in candidateRules)
            {
                if (executedRulesInContext.Contains(rule.RuleId))
                {
                    _logger.LogWarning("Rule {RuleId} already executed in correlation {CorrelationId}. Skipping to prevent duplication.", rule.RuleId, @event.CorrelationId);
                    continue;
                }

                if (_conditionEvaluator.Evaluate(rule.Condition, playthrough))
                {
                    executedRulesInContext.Add(rule.RuleId);
                    activeRules.Add(rule);
                }
            }

            if (activeRules.Count == 0) return EventHandlingResult.Ack();

            // Stage 4: Effect Calculation
            var calculatedEffects = new List<ParsedEffect>();
            foreach (var rule in activeRules)
            {
                foreach (var effectString in rule.Effects)
                {
                    var effect = _effectParser.Parse(rule.RuleId, effectString);
                    if (effect != null)
                    {
                        calculatedEffects.Add(effect);
                    }
                }
            }

            // Stage 5: Conflict Resolution
            var resolvedEffects = ResolveConflicts(calculatedEffects);

            // Stage 6: State Mutation
            // State mutation means recording audit of the evaluation context.
            // Game Engine does not persist Rule Engine state directly, so we just track in-memory during this execution phase if needed.
            var executedRuleIds = activeRules.Select(r => r.RuleId).ToList();

            // Stage 7: Derived Rule Processing
            var derivedRules = resolvedEffects.Where(e => e.Type == "DerivedRule").ToList();
            foreach (var derived in derivedRules)
            {
                var derivedTrigger = new DerivedRuleTriggeredEvent
                {
                    SessionId = @event.SessionId,
                    PlaythroughId = @event.PlaythroughId,
                    MissionId = @event.MissionId,
                    CorrelationId = @event.CorrelationId,
                    CausationId = @event.EventId,
                    OriginatingRuleId = derived.SourceRuleId,
                    DerivedRuleId = derived.Arg1,
                    Depth = (@event is DerivedRuleTriggeredEvent d) ? d.Depth + 1 : 1
                };
                await _eventBus.PublishAsync(derivedTrigger, cancellationToken);
            }

            // Stage 8: Event Publication
            await PublishConsequenceEventsAsync(resolvedEffects, @event.SessionId, @event.PlaythroughId.Value, @event.MissionId, @event.CorrelationId, @event.EventId, cancellationToken);

            // Stage 9: Audit Logging
            foreach (var rule in activeRules)
            {
                var auditEvent = new RuleExecutionAuditCreatedEvent
                {
                    SessionId = @event.SessionId,
                    PlaythroughId = @event.PlaythroughId,
                    MissionId = @event.MissionId,
                    CorrelationId = @event.CorrelationId,
                    CausationId = @event.EventId,
                    RuleId = rule.RuleId,
                    Explanation = $"Rule {rule.RuleId} executed successfully based on trigger {triggerName}."
                };
                await _eventBus.PublishAsync(auditEvent, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rule Engine execution failed.");
            return EventHandlingResult.Nack("Rule evaluation error: " + ex.Message);
        }

        return EventHandlingResult.Ack();
    }

    private static string MapEventToTrigger(DomainEvent @event)
    {
        if (@event is DecisionProcessedEvent) return "DECISION_RESOLVED";
        if (@event is DerivedRuleTriggeredEvent d) return d.DerivedRuleId;
        return string.Empty;
    }

    private static List<ParsedEffect> ResolveConflicts(List<ParsedEffect> effects)
    {
        var resolved = new List<ParsedEffect>();
        
        var metricEffects = effects.Where(e => e.Type.Equals("MetricChangeEffect", StringComparison.OrdinalIgnoreCase)).ToList();
        var summedMetrics = metricEffects.GroupBy(e => e.Arg1).Select(g => 
        {
            int total = 0;
            foreach(var effect in g)
            {
                if (int.TryParse(effect.Arg2, out int val)) total += val;
            }
            return new ParsedEffect { SourceRuleId = g.First().SourceRuleId, Type = "MetricChangeEffect", Arg1 = g.Key, Arg2 = total.ToString() };
        });
        
        resolved.AddRange(summedMetrics);

        resolved.AddRange(effects.Where(e => !e.Type.Equals("MetricChangeEffect", StringComparison.OrdinalIgnoreCase)));

        return resolved;
    }

    private async Task PublishConsequenceEventsAsync(List<ParsedEffect> effects, Guid sessionId, Guid playthroughId, string missionId, Guid correlationId, Guid? causationId, CancellationToken cancellationToken)
    {
        foreach (var effect in effects)
        {
            if (effect.Type.Equals("MetricChangeEffect", StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(effect.Arg2, out int val))
                {
                    await _eventBus.PublishAsync(new MetricDeltaAppliedEvent
                    {
                        SessionId = sessionId,
                        PlaythroughId = playthroughId,
                        MissionId = missionId,
                        CorrelationId = correlationId,
                        CausationId = causationId,
                        Metric = effect.Arg1,
                        Value = val
                    }, cancellationToken);
                }
            }
            else if (effect.Type.Equals("ArchitectureEffect", StringComparison.OrdinalIgnoreCase))
            {
                await _eventBus.PublishAsync(new ArchitectureChangedEvent
                {
                    SessionId = sessionId,
                    PlaythroughId = playthroughId,
                    MissionId = missionId,
                    CorrelationId = correlationId,
                    CausationId = causationId,
                    MutationType = effect.Arg1,
                    TargetId = effect.Arg2
                }, cancellationToken);
            }
            else if (effect.Type.Equals("TechnologyEffect", StringComparison.OrdinalIgnoreCase))
            {
                await _eventBus.PublishAsync(new TechnologyDiscoveredEvent
                {
                    SessionId = sessionId,
                    PlaythroughId = playthroughId,
                    MissionId = missionId,
                    CorrelationId = correlationId,
                    CausationId = causationId,
                    TechnologyId = effect.Arg1
                }, cancellationToken);
            }
            else if (effect.Type.Equals("EventEffect", StringComparison.OrdinalIgnoreCase))
            {
                await _eventBus.PublishAsync(new BusinessEventProcessedEvent
                {
                    SessionId = sessionId,
                    PlaythroughId = playthroughId,
                    MissionId = missionId,
                    CorrelationId = correlationId,
                    CausationId = causationId,
                    BusinessEventName = effect.Arg1
                }, cancellationToken);
            }
        }
    }

    public Task ReleaseAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
