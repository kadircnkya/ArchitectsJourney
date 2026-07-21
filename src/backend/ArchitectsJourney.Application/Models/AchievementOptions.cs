using ArchitectsJourney.Domain.Entities;

namespace ArchitectsJourney.Application.Models;

public sealed record AchievementOptions
{
    public IReadOnlyDictionary<int, int> LevelThresholds { get; init; } = new Dictionary<int, int>();
    public IReadOnlyList<Achievement> AvailableAchievements { get; init; } = [];
}
