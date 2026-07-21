using ArchitectsJourney.Domain.Common;

namespace ArchitectsJourney.Domain.ValueObjects;

public sealed record AchievementCondition
{
    public required string Type { get; init; }
    public required string TargetId { get; init; }
    public int? TargetCount { get; init; }
}
