using ArchitectsJourney.Domain.Common;
using ArchitectsJourney.Domain.ValueObjects;
using System.Text.Json.Serialization;

namespace ArchitectsJourney.Domain.Entities;

/// <summary>
/// Domain model representation of a single decision option.
/// Declares conditions for availability and complete rule consequences.
/// Defined in Document 05 and Document 08.
/// </summary>
public sealed class DecisionOption : Entity<string>
{
    public DecisionOption(string id) : base(id) { }

    [JsonPropertyName("label")]
    public required string Label { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    /// <summary>
    /// Logical condition string required to unlock this option.
    /// Example: "tech:microservices AND metric:reliability > 70"
    /// </summary>
    [JsonPropertyName("condition")]
    public string? Condition { get; init; }

    /// <summary>
    /// Direct changes applied to the simulation metrics when this option is chosen.
    /// </summary>
    [JsonPropertyName("metricImpacts")]
    public required IReadOnlyList<MetricChangeEffect> MetricImpacts { get; init; }

    /// <summary>
    /// Graph mutations triggered directly by this decision.
    /// </summary>
    [JsonPropertyName("graphMutations")]
    public required IReadOnlyList<string> GraphMutations { get; init; }
}
