using System.Text.Json.Serialization;

namespace ArchitectsJourney.Application.DTOs.Mission;

public sealed class DecisionPointDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("phase")]
    public string Phase { get; set; } = string.Empty;

    [JsonPropertyName("dialogue")]
    public string Dialogue { get; set; } = string.Empty;

    [JsonPropertyName("options")]
    public IReadOnlyList<DecisionOptionDto> Options { get; init; } = Array.Empty<DecisionOptionDto>();
}
