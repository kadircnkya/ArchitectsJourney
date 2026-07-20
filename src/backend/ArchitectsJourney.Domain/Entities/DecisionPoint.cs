using ArchitectsJourney.Domain.Common;
using ArchitectsJourney.Domain.Entities;
using ArchitectsJourney.Domain.Enums;
using System.Text.Json.Serialization;

namespace ArchitectsJourney.Domain.Entities;

/// <summary>
/// Represents an authored decision point where players make choices.
/// Belongs to the Mission aggregate root.
/// Defined in Document 05 and Document 08.
/// </summary>
public sealed class DecisionPoint : Entity<string>
{
    public DecisionPoint(string id) : base(id) { }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("phase")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required MissionPhase Phase { get; init; }

    [JsonPropertyName("dialogue")]
    public required string Dialogue { get; init; }

    [JsonPropertyName("options")]
    public required IReadOnlyList<DecisionOption> Options { get; init; }
}
