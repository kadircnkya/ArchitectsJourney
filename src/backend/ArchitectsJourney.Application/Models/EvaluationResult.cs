using ArchitectsJourney.Domain.Enums;
using ArchitectsJourney.Domain.ValueObjects;

namespace ArchitectsJourney.Application.Models;

public sealed record EvaluationResult
{
    public required int TotalScore { get; init; }
    public required MissionRank Rank { get; init; }
    public required MissionResult MissionResult { get; init; }
    public required bool Passed { get; init; }
    public required IReadOnlyList<string> Comments { get; init; }
}
