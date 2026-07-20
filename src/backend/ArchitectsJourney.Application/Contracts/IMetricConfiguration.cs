namespace ArchitectsJourney.Application.Contracts;

/// <summary>
/// Provides configuration for the Metric Engine.
/// Configurable values such as thresholds to avoid hardcoded numbers.
/// </summary>
public interface IMetricConfiguration
{
    /// <summary>
    /// Gets the thresholds at which MetricThresholdCrossedEvent should be triggered.
    /// Example: [25, 50, 75]
    /// </summary>
    IReadOnlyList<int> Thresholds { get; }
}
