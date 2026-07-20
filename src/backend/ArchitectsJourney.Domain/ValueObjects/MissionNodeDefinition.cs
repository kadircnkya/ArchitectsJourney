using ArchitectsJourney.Domain.Common;
using System.Text.Json.Serialization;

namespace ArchitectsJourney.Domain.ValueObjects;

/// <summary>
/// Logical Node component definition inside a Mission definition.
/// Part of the InitialArchitectureState authored schema (Doc 08, Section 9).
/// </summary>
public sealed class MissionNodeDefinition : ValueObject
{
    [JsonPropertyName("id")]
    public required string NodeId { get; init; }

    [JsonPropertyName("type")]
    public required string NodeType { get; init; }

    [JsonPropertyName("label")]
    public required string Label { get; init; }

    [JsonPropertyName("technologyId")]
    public string? TechnologyId { get; init; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return NodeId;
        yield return NodeType;
        yield return Label;
        yield return TechnologyId;
    }
}
