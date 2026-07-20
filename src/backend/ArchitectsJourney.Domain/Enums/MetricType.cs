namespace ArchitectsJourney.Domain.Enums;

/// <summary>
/// The seven metrics tracked throughout every mission.
/// Defined in Document 05 (Domain Model) and Document 11 (Metric Model).
/// COMPLEXITY follows inverted semantics: higher value = greater complexity burden.
/// COST follows inverted semantics: higher value = lower cost burden.
/// </summary>
public enum MetricType
{
    Performance,
    Scalability,
    Reliability,
    Maintainability,
    Complexity,
    Cost,
    Security
}
