using System.Collections.Generic;
using ArchitectsJourney.Domain.Entities;
using ArchitectsJourney.Domain.ValueObjects;

namespace ArchitectsJourney.Application.Contracts;

public sealed record ArchitectureValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }

    public static ArchitectureValidationResult Success() => new() { IsValid = true };
    public static ArchitectureValidationResult Failure(string message) => new() { IsValid = false, ErrorMessage = message };
}

/// <summary>
/// Strictly read-only validator for the architecture graph.
/// Does not mutate the graph under any circumstances.
/// </summary>
public interface IArchitectureValidator
{
    ArchitectureValidationResult ValidateNodeAddition(IReadOnlyList<ArchitectureNode> currentNodes, IReadOnlyList<ArchitectureEdge> currentEdges, ArchitectureNode nodeToAdd);
    ArchitectureValidationResult ValidateNodeRemoval(IReadOnlyList<ArchitectureNode> currentNodes, IReadOnlyList<ArchitectureEdge> currentEdges, string nodeIdToRemove);
    ArchitectureValidationResult ValidateEdgeAddition(IReadOnlyList<ArchitectureNode> currentNodes, IReadOnlyList<ArchitectureEdge> currentEdges, ArchitectureEdge edgeToAdd);
    ArchitectureValidationResult ValidateEdgeRemoval(IReadOnlyList<ArchitectureNode> currentNodes, IReadOnlyList<ArchitectureEdge> currentEdges, string sourceNodeId, string targetNodeId, string edgeType);
}
