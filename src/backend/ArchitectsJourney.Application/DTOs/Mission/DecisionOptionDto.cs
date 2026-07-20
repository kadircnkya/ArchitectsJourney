using System.Text.Json.Serialization;

namespace ArchitectsJourney.Application.DTOs.Mission;

public sealed class DecisionOptionDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("condition")]
    public string? Condition { get; set; }

    [JsonPropertyName("metricImpacts")]
    public IReadOnlyList<MetricChangeEffectDto> MetricImpacts { get; init; } = Array.Empty<MetricChangeEffectDto>();

    [JsonPropertyName("graphMutations")]
    public IReadOnlyList<string> GraphMutations { get; init; } = Array.Empty<string>();
}
