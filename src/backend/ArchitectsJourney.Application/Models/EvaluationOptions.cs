namespace ArchitectsJourney.Application.Models;

public sealed record EvaluationOptions
{
    public int MetricWeight { get; init; } = 1;
    public int ObjectiveWeight { get; init; } = 100;
    public int TechnologyWeight { get; init; } = 10;
    public int ArchitectureWeight { get; init; } = 10;
    public int BonusMultiplier { get; init; } = 2;
    public int PenaltyMultiplier { get; init; } = 2;

    public int BronzeThreshold { get; init; } = 100;
    public int SilverThreshold { get; init; } = 300;
    public int GoldThreshold { get; init; } = 600;
    public int PlatinumThreshold { get; init; } = 1000;
}
