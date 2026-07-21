using ArchitectsJourney.Application.Models;
using ArchitectsJourney.Domain.Aggregates;

namespace ArchitectsJourney.Application.Contracts;

public interface IAchievementEvaluator
{
    AchievementEvaluationResult Evaluate(Playthrough playthrough, AchievementOptions options);
}
