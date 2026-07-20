using ArchitectsJourney.Domain.Common;
using ArchitectsJourney.Domain.Enums;
using System.Text.Json.Serialization;

namespace ArchitectsJourney.Domain.ValueObjects;

/// <summary>
/// Metric delta change specification authored inside mission content.
/// Implements Metric Balancing Framework constraints described in Document 07, Section 3.
/// </summary>
public sealed class MetricChangeEffect : ValueObject
{
    [JsonPropertyName("metric")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required MetricType Metric { get; init; }

    [JsonPropertyName("label")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required DeltaLabel Label { get; init; }

    [JsonPropertyName("value")]
    public required int Value { get; init; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Metric;
        yield return Label;
        yield return Value;
    }
}
