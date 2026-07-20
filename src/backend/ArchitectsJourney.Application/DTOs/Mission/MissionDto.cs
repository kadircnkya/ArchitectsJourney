using System.Text.Json.Serialization;

namespace ArchitectsJourney.Application.DTOs.Mission;

public sealed class MissionDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("initialMetrics")]
    public IReadOnlyDictionary<string, int> InitialMetrics { get; init; } = new Dictionary<string, int>();

    [JsonPropertyName("initialNodes")]
    public IReadOnlyList<MissionNodeDto> InitialNodes { get; init; } = Array.Empty<MissionNodeDto>();

    [JsonPropertyName("initialEdges")]
    public IReadOnlyList<MissionEdgeDto> InitialEdges { get; init; } = Array.Empty<MissionEdgeDto>();

    [JsonPropertyName("decisionPoints")]
    public IReadOnlyList<DecisionPointDto> DecisionPoints { get; init; } = Array.Empty<DecisionPointDto>();

    [JsonPropertyName("rules")]
    public IReadOnlyList<MissionRuleDto> Rules { get; init; } = Array.Empty<MissionRuleDto>();
}
