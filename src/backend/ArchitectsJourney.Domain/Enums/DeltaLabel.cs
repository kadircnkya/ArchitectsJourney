namespace ArchitectsJourney.Domain.Enums;

/// <summary>
/// Metric delta magnitude labels for mission rule authoring.
/// Used in Document 08 (Mission JSON) MetricChangeEffect.
/// </summary>
public enum DeltaLabel
{
    MajorImprovement,
    MinorImprovement,
    Neutral,
    MinorDegradation,
    MajorDegradation
}
