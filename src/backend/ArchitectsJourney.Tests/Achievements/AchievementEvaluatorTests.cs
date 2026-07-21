using ArchitectsJourney.Application.Models;
using ArchitectsJourney.Domain.Aggregates;
using ArchitectsJourney.Domain.Entities;
using ArchitectsJourney.Domain.ValueObjects;
using ArchitectsJourney.Infrastructure.Achievements;

namespace ArchitectsJourney.Tests.Achievements;

public class AchievementEvaluatorTests
{
    private readonly AchievementEvaluator _sut = new();

    [Fact]
    public void Evaluate_Deterministic_Evaluation()
    {
        var pt = new Playthrough(Guid.NewGuid(), "M1");
        
        var options = new AchievementOptions
        {
            AvailableAchievements = [
                new Achievement("ACH_1", "Test", "Test Desc", "General", 100, false, [
                    new AchievementCondition { Type = "OBJECTIVE_COMPLETED", TargetId = "OBJ1" }
                ])
            ]
        };

        var res1 = _sut.Evaluate(pt, options);
        var res2 = _sut.Evaluate(pt, options);

        Assert.Empty(res1.UnlockedAchievements);
        Assert.Empty(res2.UnlockedAchievements);
        Assert.Equal(0, res1.AwardedExperience);
        Assert.Equal(0, res2.AwardedExperience);
    }

    [Fact]
    public void Evaluate_ObjectiveCompleted_UnlocksAchievementAndAwardsExp()
    {
        var pt = new Playthrough(Guid.NewGuid(), "M1");
        pt.InitializeObjectives(["OBJ1"]);
        pt.CompleteObjective("OBJ1");

        var options = new AchievementOptions
        {
            AvailableAchievements = [
                new Achievement("ACH_1", "Test", "Test Desc", "General", 100, false, [
                    new AchievementCondition { Type = "OBJECTIVE_COMPLETED", TargetId = "OBJ1" }
                ])
            ],
            LevelThresholds = new Dictionary<int, int> { { 2, 50 } }
        };

        var result = _sut.Evaluate(pt, options);

        Assert.Contains("ACH_1", result.UnlockedAchievements);
        Assert.Equal(100, result.AwardedExperience);
        Assert.Equal(2, result.NewLevel);
    }

    [Fact]
    public void Evaluate_DuplicateUnlock_Prevented()
    {
        var pt = new Playthrough(Guid.NewGuid(), "M1");
        pt.InitializeObjectives(["OBJ1"]);
        pt.CompleteObjective("OBJ1");
        pt.UnlockAchievement("ACH_1");

        var options = new AchievementOptions
        {
            AvailableAchievements = [
                new Achievement("ACH_1", "Test", "Test Desc", "General", 100, false, [
                    new AchievementCondition { Type = "OBJECTIVE_COMPLETED", TargetId = "OBJ1" }
                ])
            ]
        };

        var result = _sut.Evaluate(pt, options);

        Assert.Empty(result.UnlockedAchievements);
        Assert.Equal(0, result.AwardedExperience);
    }
}
