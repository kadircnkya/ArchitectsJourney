using ArchitectsJourney.Application.Models;
using ArchitectsJourney.Domain.Aggregates;
using ArchitectsJourney.Domain.Entities;
using ArchitectsJourney.Domain.Enums;
using ArchitectsJourney.Infrastructure.Scoring;
using Xunit;

namespace ArchitectsJourney.Tests.Scoring;

public class ScoreCalculatorTests
{
    private readonly ScoreCalculator _sut;
    private readonly EvaluationOptions _options;

    public ScoreCalculatorTests()
    {
        _sut = new ScoreCalculator();
        _options = new EvaluationOptions
        {
            MetricWeight = 1,
            ObjectiveWeight = 100,
            TechnologyWeight = 10,
            ArchitectureWeight = 10,
            BonusMultiplier = 2,
            PenaltyMultiplier = 2,
            BronzeThreshold = 100,
            SilverThreshold = 300,
            GoldThreshold = 600,
            PlatinumThreshold = 1000
        };
    }

    [Fact]
    public void Calculate_WithVariousInputs_ReturnsDeterministicScore()
    {
        // Arrange
        var pt = new Playthrough(Guid.NewGuid(), "m1");
        pt.InitializeMetrics(new Dictionary<MetricType, int>
        {
            { MetricType.Performance, 50 },
            { MetricType.Cost, 30 }
        }); // sum = 80 -> metric score = 80
        
        pt.InitializeObjectives(["obj1", "obj2", "obj3"]);
        pt.CompleteObjective("obj1");
        pt.FailObjective("obj2");
        // obj1 complete -> +100
        // obj2 failed -> -200 (penalty)
        // obj3 pending -> 0
        
        pt.DiscoverTechnology("tech1");
        pt.DiscoverTechnology("tech2"); // 2 techs -> +20

        pt.AddNode(new ArchitectureNode("n1", "type", "label", null)); // 1 node -> +10

        pt.ResolveDecision("dp1", "opt1"); // 1 decision -> bonus = 2

        // Total: 80 + 100 + 20 + 10 + 2 - 200 = 12
        
        // Act
        var result = _sut.Calculate(pt, _options);


        Assert.Equal(12, result.TotalScore);
        
        Assert.Equal(MissionRank.None, result.Rank);
        Assert.False(result.Passed);
        Assert.Equal(MissionResult.Failure, result.MissionResult);
    }

    [Fact]
    public void Calculate_ScoreBelowZero_IsClampedToZero()
    {
        var pt = new Playthrough(Guid.NewGuid(), "m1");
        pt.InitializeObjectives(["obj1"]);
        pt.FailObjective("obj1");
        
        var result = _sut.Calculate(pt, _options);
        
        Assert.Equal(0, result.TotalScore);
    }
    
    [Theory]
    [InlineData(50, MissionRank.None)]
    [InlineData(100, MissionRank.Bronze)]
    [InlineData(350, MissionRank.Silver)]
    [InlineData(600, MissionRank.Gold)]
    [InlineData(1500, MissionRank.Platinum)]
    public void Calculate_RankAssignment_RespectsThresholds(int score, MissionRank expectedRank)
    {
        var pt = new Playthrough(Guid.NewGuid(), "m1");
        pt.InitializeMetrics(new Dictionary<MetricType, int> { { MetricType.Performance, score } });
        
        var result = _sut.Calculate(pt, _options);
        
        Assert.Equal(expectedRank, result.Rank);
    }
}
