using System.Text.Json.Serialization;

namespace ArchitectsJourney.Application.DTOs.Mission;

public sealed class MissionNodeDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("technologyId")]
    public string? TechnologyId { get; set; }
}
