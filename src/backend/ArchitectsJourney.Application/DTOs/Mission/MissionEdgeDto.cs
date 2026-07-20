using System.Text.Json.Serialization;

namespace ArchitectsJourney.Application.DTOs.Mission;

public sealed class MissionEdgeDto
{
    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("communication")]
    public string Communication { get; set; } = string.Empty;
}
