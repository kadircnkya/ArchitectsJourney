using System.Text.Json.Serialization;

namespace ArchitectsJourney.Application.DTOs.Mission;

public sealed class MissionRuleDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("trigger")]
    public string Trigger { get; set; } = string.Empty;

    [JsonPropertyName("condition")]
    public string? Condition { get; set; }

    [JsonPropertyName("effects")]
    public IReadOnlyList<string> Effects { get; init; } = Array.Empty<string>();
}
