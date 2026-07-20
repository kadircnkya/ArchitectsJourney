using ArchitectsJourney.Domain.Aggregates;
using ArchitectsJourney.Domain.Enums;

namespace ArchitectsJourney.Engines.Rule.Parsing;

public sealed class StringCompatibilityConditionEvaluator : IConditionEvaluator
{
    public bool Evaluate(string? condition, Playthrough playthrough)
    {
        ArgumentNullException.ThrowIfNull(playthrough);

        if (string.IsNullOrWhiteSpace(condition)) return true;

        string[] sep = [" AND "];
        var clauses = condition.Split(sep, StringSplitOptions.None);
        foreach (var clause in clauses)
        {
            var trimmed = clause.Trim();
            bool result = EvaluateClause(trimmed, playthrough);
            if (!result) return false; 
        }

        return true;
    }

    private static bool EvaluateClause(string clause, Playthrough playthrough)
    {
        if (clause.StartsWith("tech:", StringComparison.OrdinalIgnoreCase))
        {
            var tech = clause.Substring(5).Trim();
            return playthrough.UnlockedTechnologies.Contains(tech);
        }
        if (clause.StartsWith("metric:", StringComparison.OrdinalIgnoreCase))
        {
            char[] separators = ['>', '<', '='];
            var parts = clause.Substring(7).Split(separators, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) return false;

            var metricStr = parts[0].Trim();
            var valStr = parts[1].Trim().Trim('='); 

            if (!Enum.TryParse<MetricType>(metricStr, true, out var metricType)) return false;
            if (!int.TryParse(valStr, out int targetValue)) return false;

            playthrough.Metrics.TryGetValue(metricType, out int currentValue);

            if (clause.Contains(">=", StringComparison.Ordinal)) return currentValue >= targetValue;
            if (clause.Contains("<=", StringComparison.Ordinal)) return currentValue <= targetValue;
            if (clause.Contains('>', StringComparison.Ordinal)) return currentValue > targetValue;
            if (clause.Contains('<', StringComparison.Ordinal)) return currentValue < targetValue;
            if (clause.Contains('=', StringComparison.Ordinal)) return currentValue == targetValue;
        }
        return false;
    }
}
