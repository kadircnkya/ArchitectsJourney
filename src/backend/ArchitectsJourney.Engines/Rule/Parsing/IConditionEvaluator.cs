using ArchitectsJourney.Domain.Aggregates;

namespace ArchitectsJourney.Engines.Rule.Parsing;

public interface IConditionEvaluator
{
    bool Evaluate(string? condition, Playthrough playthrough);
}
