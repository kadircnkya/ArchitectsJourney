using ArchitectsJourney.Domain.Aggregates;
using ArchitectsJourney.Domain.Enums;
using System.Text.RegularExpressions;

namespace ArchitectsJourney.Engines.Rule;

/// <summary>
/// Service that evaluates declarative logic condition strings against active Playthrough state.
/// Supports metrics and technology checks combined with AND/OR logical operators.
/// Example: "tech:Microservices AND metric:Reliability > 50"
/// </summary>
public static class ConditionEvaluator
{
    private static readonly Regex MetricRegex = new(@"metric:(?<name>\w+)\s*(?<op>>|<|=)\s*(?<value>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TechRegex = new(@"tech:(?<id>\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool Evaluate(string? condition, Playthrough playthrough)
    {
        ArgumentNullException.ThrowIfNull(playthrough);

        if (string.IsNullOrWhiteSpace(condition))
        {
            return true; // No condition means always available / true
        }

        // Handle AND logic split
        if (condition.Contains(" AND ", StringComparison.OrdinalIgnoreCase))
        {
            var parts = condition.Split(" AND ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.All(p => EvaluateSingleCondition(p, playthrough));
        }

        // Handle OR logic split
        if (condition.Contains(" OR ", StringComparison.OrdinalIgnoreCase))
        {
            var parts = condition.Split(" OR ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return parts.Any(p => EvaluateSingleCondition(p, playthrough));
        }

        return EvaluateSingleCondition(condition, playthrough);
    }

    private static bool EvaluateSingleCondition(string subCondition, Playthrough playthrough)
    {
        // 1. Metric check matching: metric:Name > value
        var metricMatch = MetricRegex.Match(subCondition);
        if (metricMatch.Success)
        {
            var nameStr = metricMatch.Groups["name"].Value;
            var opStr = metricMatch.Groups["op"].Value;
            var valStr = metricMatch.Groups["value"].Value;

            if (!Enum.TryParse<MetricType>(nameStr, true, out var metricType))
            {
                return false; // Unknown metric type evaluates to false
            }

            int targetValue = int.Parse(valStr);
            playthrough.Metrics.TryGetValue(metricType, out int actualValue);

            return opStr switch
            {
                ">" => actualValue > targetValue,
                "<" => actualValue < targetValue,
                "=" => actualValue == targetValue,
                _ => false
            };
        }

        // 2. Technology discovered matching: tech:id
        var techMatch = TechRegex.Match(subCondition);
        if (techMatch.Success)
        {
            var techId = techMatch.Groups["id"].Value;
            return playthrough.DiscoveredTechnologies.Contains(techId, StringComparer.OrdinalIgnoreCase);
        }

        return false;
    }
}
