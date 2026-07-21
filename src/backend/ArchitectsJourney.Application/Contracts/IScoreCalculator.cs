using ArchitectsJourney.Domain.Aggregates;
using ArchitectsJourney.Application.Models;

namespace ArchitectsJourney.Application.Contracts;

public interface IScoreCalculator
{
    EvaluationResult Calculate(Playthrough playthrough, EvaluationOptions options);
}
