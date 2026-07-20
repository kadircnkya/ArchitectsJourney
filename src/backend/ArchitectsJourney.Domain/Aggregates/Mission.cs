using ArchitectsJourney.Domain.Common;
using ArchitectsJourney.Domain.Entities;
using ArchitectsJourney.Domain.ValueObjects;
using System.Text.Json.Serialization;

namespace ArchitectsJourney.Domain.Aggregates;

/// <summary>
/// Mission aggregate root representing authored static content.
/// Corresponds to the Mission JSON schema in Document 08.
/// Contains all baseline node structures, metrics, decision trees, and rules.
/// </summary>
public sealed class Mission : AggregateRoot<string>
{
    public Mission(string id) : base(id) { }

    [JsonPropertyName("version")]
    public required string Version { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("description")]
    public required string Description { get; init; }

    [JsonPropertyName("initialMetrics")]
    public required IReadOnlyDictionary<string, int> InitialMetrics { get; init; }

    [JsonPropertyName("initialNodes")]
    public required IReadOnlyList<MissionNodeDefinition> InitialNodes { get; init; }

    [JsonPropertyName("initialEdges")]
    public required IReadOnlyList<MissionEdgeDefinition> InitialEdges { get; init; }

    [JsonPropertyName("decisionPoints")]
    public required IReadOnlyList<DecisionPoint> DecisionPoints { get; init; }

    [JsonPropertyName("rules")]
    public required IReadOnlyList<MissionRuleDefinition> Rules { get; init; }
}

/// <summary>
/// Helper class representing authored rule definitions in the mission JSON file.
/// </summary>
public sealed class MissionRuleDefinition : ValueObject
{
    [JsonPropertyName("id")]
    public required string RuleId { get; init; }

    [JsonPropertyName("trigger")]
    public required string Trigger { get; init; }

    [JsonPropertyName("condition")]
    public string? Condition { get; init; }

    [JsonPropertyName("effects")]
    public required IReadOnlyList<string> Effects { get; init; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return RuleId;
        yield return Trigger;
        yield return Condition;
    }
}
