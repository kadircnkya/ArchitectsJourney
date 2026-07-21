using ArchitectsJourney.Domain.Common;
using System.Text.Json.Serialization;

namespace ArchitectsJourney.Domain.ValueObjects;

public sealed class ObjectiveCondition : ValueObject
{
    [JsonPropertyName("type")]
    public required string Type { get; init; } // e.g., "Metric", "ArchitectureNode"

    [JsonPropertyName("target")]
    public required string Target { get; init; } // e.g., "Cost", "API Gateway"

    [JsonPropertyName("operator")]
    public required string Operator { get; init; } // e.g., ">=", "Contains"

    [JsonPropertyName("value")]
    public required string Value { get; init; } // e.g., "50", "true"

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return Target;
        yield return Operator;
        yield return Value;
    }
}
