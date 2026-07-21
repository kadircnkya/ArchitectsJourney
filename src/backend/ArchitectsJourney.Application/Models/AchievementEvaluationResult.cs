namespace ArchitectsJourney.Application.Models;

public sealed record AchievementEvaluationResult
{
    public required IReadOnlyList<string> UnlockedAchievements { get; init; }
    public required int AwardedExperience { get; init; }
    public required int NewLevel { get; init; }
    public required IReadOnlyList<string> Notifications { get; init; }
}
