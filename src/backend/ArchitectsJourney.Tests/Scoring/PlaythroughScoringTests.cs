using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Application.Events.System;
using ArchitectsJourney.Application.Models;
using ArchitectsJourney.Domain.Aggregates;
using ArchitectsJourney.Domain.Enums;
using ArchitectsJourney.Engines.Scoring;
using ArchitectsJourney.Infrastructure.EventTracker;
using ArchitectsJourney.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ArchitectsJourney.Tests.Scoring;

public class PlaythroughScoringTests
{
    [Fact]
    public void Snapshot_GenerationAndRestoration_PreservesEvaluationState()
    {
        var pt = new Playthrough(Guid.NewGuid(), "m1");
        pt.UpdateScore(450);
        pt.CompleteEvaluation(MissionRank.Gold, MissionResult.Success, DateTimeOffset.UtcNow);

        var snapshot = pt.TakeSnapshot();
        
        var ptRestored = new Playthrough(pt.Id, "m2"); // ID must match, other properties will be overwritten
        ptRestored.Restore(snapshot);

        Assert.Equal(450, ptRestored.CurrentScore);
        Assert.Equal(MissionRank.Gold, ptRestored.FinalRank);
        Assert.Equal(MissionResult.Success, ptRestored.MissionResult);
        Assert.True(ptRestored.EvaluationCompleted);
    }
}
