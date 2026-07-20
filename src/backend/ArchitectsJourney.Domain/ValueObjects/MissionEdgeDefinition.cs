using ArchitectsJourney.Domain.Common;
using ArchitectsJourney.Domain.Enums;
using System.Text.Json.Serialization;

namespace ArchitectsJourney.Domain.ValueObjects;

/// <summary>
/// Logical Edge definition connecting baseline architecture components in a Mission.
/// Part of the InitialArchitectureState authored schema (Doc 08, Section 9).
/// </summary>
public sealed class MissionEdgeDefinition : ValueObject
{
    [JsonPropertyName("source")]
    public required string Source { get; init; }

    [JsonPropertyName("target")]
    public required string Target { get; init; }

    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required EdgeType Type { get; init; }

    [JsonPropertyName("communication")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required CommunicationType Communication { get; init; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Source;
        yield return Target;
        yield return Type;
        yield return Communication;
    }
}
