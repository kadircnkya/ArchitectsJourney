using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Application.Models;
using ArchitectsJourney.Domain.Aggregates;
using ArchitectsJourney.Domain.Entities;
using ArchitectsJourney.Domain.Enums;
using ArchitectsJourney.Domain.ValueObjects;

namespace ArchitectsJourney.Infrastructure.Scoring;

public class ScoreCalculator : IScoreCalculator
{
    public EvaluationResult Calculate(Playthrough playthrough, EvaluationOptions options)
    {
        ArgumentNullException.ThrowIfNull(playthrough);
        ArgumentNullException.ThrowIfNull(options);

        int metricScore = CalculateMetricScore(playthrough, options);
        int objectiveScore = CalculateObjectiveScore(playthrough, options);
        int technologyScore = CalculateTechnologyScore(playthrough, options);
        int architectureScore = CalculateArchitectureScore(playthrough, options);
        
        int bonusScore = CalculateBonusScore(playthrough, options);
        int penaltyScore = CalculatePenaltyScore(playthrough, options);

        int totalScore = metricScore + objectiveScore + technologyScore + architectureScore + bonusScore - penaltyScore;
        totalScore = Math.Max(0, totalScore);

        var rank = DetermineRank(totalScore, options);
        bool passed = rank >= MissionRank.Bronze;
        var missionResult = passed ? MissionResult.Success : MissionResult.Failure;

        var comments = new List<string>();
        if (!passed)
        {
            comments.Add("Mission failed to reach minimum threshold.");
        }

        return new EvaluationResult
        {
            TotalScore = totalScore,
            Rank = rank,
            MissionResult = missionResult,
            Passed = passed,
            Comments = comments
        };
    }

    private static int CalculateMetricScore(Playthrough playthrough, EvaluationOptions options)
    {
        return playthrough.Metrics.Values.Sum() * options.MetricWeight;
    }

    private static int CalculateObjectiveScore(Playthrough playthrough, EvaluationOptions options)
    {
        int count = playthrough.Objectives.Count(o => o.State == ObjectiveState.Completed);
        return count * options.ObjectiveWeight;
    }

    private static int CalculateTechnologyScore(Playthrough playthrough, EvaluationOptions options)
    {
        return playthrough.DiscoveredTechnologies.Count * options.TechnologyWeight;
    }

    private static int CalculateArchitectureScore(Playthrough playthrough, EvaluationOptions options)
    {
        return playthrough.Nodes.Count * options.ArchitectureWeight;
    }

    private static int CalculateBonusScore(Playthrough playthrough, EvaluationOptions options)
    {
        // Simple example: bonus for resolving all decisions early, or just dummy calculation
        return (playthrough.ResolvedDecisions.Count * options.BonusMultiplier);
    }

    private static int CalculatePenaltyScore(Playthrough playthrough, EvaluationOptions options)
    {
        int failedObjectives = playthrough.Objectives.Count(o => o.State == ObjectiveState.Failed);
        return failedObjectives * options.PenaltyMultiplier * options.ObjectiveWeight; // arbitrary penalty logic
    }

    private static MissionRank DetermineRank(int totalScore, EvaluationOptions options)
    {
        if (totalScore >= options.PlatinumThreshold) return MissionRank.Platinum;
        if (totalScore >= options.GoldThreshold) return MissionRank.Gold;
        if (totalScore >= options.SilverThreshold) return MissionRank.Silver;
        if (totalScore >= options.BronzeThreshold) return MissionRank.Bronze;
        return MissionRank.None;
    }
}
