using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Application.Models;
using ArchitectsJourney.Domain.Aggregates;

namespace ArchitectsJourney.Infrastructure.Achievements;

public class AchievementEvaluator : IAchievementEvaluator
{
    public AchievementEvaluationResult Evaluate(Playthrough playthrough, AchievementOptions options)
    {
        ArgumentNullException.ThrowIfNull(playthrough);
        ArgumentNullException.ThrowIfNull(options);

        var newlyUnlocked = new List<string>();
        int totalExpToAward = 0;
        var notifications = new List<string>();

        foreach (var achievement in options.AvailableAchievements)
        {
            if (playthrough.UnlockedAchievements.Contains(achievement.Id))
                continue;

            bool isUnlocked = EvaluateConditions(achievement, playthrough);
            
            if (isUnlocked)
            {
                newlyUnlocked.Add(achievement.Id);
                totalExpToAward += achievement.ExperienceReward;
                notifications.Add($"Achievement Unlocked: {achievement.Name}");
            }
        }

        int currentExp = playthrough.ExperiencePoints + totalExpToAward;
        int newLevel = playthrough.PlayerLevel;

        while (options.LevelThresholds.TryGetValue(newLevel + 1, out int expRequired) && currentExp >= expRequired)
        {
            newLevel++;
            notifications.Add($"Leveled Up! You are now level {newLevel}.");
        }

        return new AchievementEvaluationResult
        {
            UnlockedAchievements = newlyUnlocked,
            AwardedExperience = totalExpToAward,
            NewLevel = newLevel,
            Notifications = notifications
        };
    }

    private static bool EvaluateConditions(Domain.Entities.Achievement achievement, Playthrough playthrough)
    {
        if (achievement.Conditions.Count == 0) return false;

        foreach (var condition in achievement.Conditions)
        {
            switch (condition.Type.ToUpperInvariant())
            {
                case "OBJECTIVE_COMPLETED":
                    if (!playthrough.Objectives.Any(o => o.Id == condition.TargetId && o.State == Domain.Entities.ObjectiveState.Completed))
                        return false;
                    break;
                case "TECHNOLOGY_DISCOVERED":
                    if (!playthrough.DiscoveredTechnologies.Contains(condition.TargetId))
                        return false;
                    break;
                case "NODE_COUNT_GREATER_THAN":
                    if (playthrough.Nodes.Count(n => n.TechnologyId == condition.TargetId) < (condition.TargetCount ?? 1))
                        return false;
                    break;
                case "METRIC_ABOVE":
                    if (Enum.TryParse<Domain.Enums.MetricType>(condition.TargetId, true, out var metricType))
                    {
                        if (!playthrough.Metrics.TryGetValue(metricType, out int val) || val <= (condition.TargetCount ?? 0))
                            return false;
                    }
                    else return false;
                    break;
                default:
                    return false;
            }
        }

        return true;
    }
}
