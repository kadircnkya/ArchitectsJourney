using ArchitectsJourney.Domain.Common;
using System.Text.Json.Serialization;

namespace ArchitectsJourney.Domain.ValueObjects;

public sealed class MissionObjectiveDefinition : ValueObject
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("conditions")]
    public required IReadOnlyList<ObjectiveCondition> Conditions { get; init; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Id;
        yield return Description;
        foreach (var c in Conditions) yield return c;
    }
}
