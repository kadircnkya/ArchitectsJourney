using System.Collections.Generic;
using System.Linq;
using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Domain.Entities;
using ArchitectsJourney.Domain.ValueObjects;

namespace ArchitectsJourney.Infrastructure.Architecture;

public sealed class ArchitectureValidator : IArchitectureValidator
{
    private readonly ArchitectureValidationOptions _options;

    public ArchitectureValidator(ArchitectureValidationOptions options)
    {
        _options = options;
    }

    public ArchitectureValidationResult ValidateNodeAddition(IReadOnlyList<ArchitectureNode> currentNodes, IReadOnlyList<ArchitectureEdge> currentEdges, ArchitectureNode nodeToAdd)
    {
        System.ArgumentNullException.ThrowIfNull(nodeToAdd);

        if (currentNodes.Any(n => n.Id == nodeToAdd.Id))
        {
            return ArchitectureValidationResult.Failure($"Node with ID {nodeToAdd.Id} already exists.");
        }

        if (_options.RequireStrictTechAssignments && string.IsNullOrEmpty(nodeToAdd.TechnologyId))
        {
            return ArchitectureValidationResult.Failure("Node must have a technology assigned.");
        }

        return ArchitectureValidationResult.Success();
    }

    public ArchitectureValidationResult ValidateNodeRemoval(IReadOnlyList<ArchitectureNode> currentNodes, IReadOnlyList<ArchitectureEdge> currentEdges, string nodeIdToRemove)
    {
        if (!currentNodes.Any(n => n.Id == nodeIdToRemove))
        {
            return ArchitectureValidationResult.Failure($"Node with ID {nodeIdToRemove} does not exist.");
        }

        if (!_options.AllowDanglingEdges && currentEdges.Any(e => e.Source == nodeIdToRemove || e.Target == nodeIdToRemove))
        {
            return ArchitectureValidationResult.Failure($"Node {nodeIdToRemove} cannot be removed because it has connected edges.");
        }

        return ArchitectureValidationResult.Success();
    }

    public ArchitectureValidationResult ValidateEdgeAddition(IReadOnlyList<ArchitectureNode> currentNodes, IReadOnlyList<ArchitectureEdge> currentEdges, ArchitectureEdge edgeToAdd)
    {
        System.ArgumentNullException.ThrowIfNull(currentEdges);
        System.ArgumentNullException.ThrowIfNull(edgeToAdd);

        if (currentEdges.Any(e => e.Source == edgeToAdd.Source && e.Target == edgeToAdd.Target && e.Type == edgeToAdd.Type))
        {
            return ArchitectureValidationResult.Failure("Edge already exists.");
        }

        if (!currentNodes.Any(n => n.Id == edgeToAdd.Source))
        {
            return ArchitectureValidationResult.Failure($"Source node {edgeToAdd.Source} does not exist.");
        }

        if (!currentNodes.Any(n => n.Id == edgeToAdd.Target))
        {
            return ArchitectureValidationResult.Failure($"Target node {edgeToAdd.Target} does not exist.");
        }

        if (!_options.AllowCycles)
        {
            if (CreatesCycle(currentEdges, edgeToAdd))
            {
                return ArchitectureValidationResult.Failure("Adding this edge would create a cycle, which is not allowed.");
            }
        }

        return ArchitectureValidationResult.Success();
    }

    public ArchitectureValidationResult ValidateEdgeRemoval(IReadOnlyList<ArchitectureNode> currentNodes, IReadOnlyList<ArchitectureEdge> currentEdges, string sourceNodeId, string targetNodeId, string edgeType)
    {
        if (!currentEdges.Any(e => e.Source == sourceNodeId && e.Target == targetNodeId && e.Type.ToString() == edgeType))
        {
            return ArchitectureValidationResult.Failure("Edge does not exist.");
        }

        return ArchitectureValidationResult.Success();
    }

    private static bool CreatesCycle(IReadOnlyList<ArchitectureEdge> edges, ArchitectureEdge newEdge)
    {
        var adjacencyList = new Dictionary<string, List<string>>();

        // Build adjacency list
        foreach (var edge in edges)
        {
            if (!adjacencyList.TryGetValue(edge.Source, out var list))
            {
                list = new List<string>();
                adjacencyList[edge.Source] = list;
            }
            list.Add(edge.Target);
        }

        if (!adjacencyList.TryGetValue(newEdge.Source, out var newList))
        {
            newList = new List<string>();
            adjacencyList[newEdge.Source] = newList;
        }
        newList.Add(newEdge.Target);

        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();

        foreach (var node in adjacencyList.Keys)
        {
            if (HasCycle(node, adjacencyList, visited, recursionStack))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasCycle(string node, Dictionary<string, List<string>> adjacencyList, HashSet<string> visited, HashSet<string> recursionStack)
    {
        if (recursionStack.Contains(node))
            return true;

        if (visited.Contains(node))
            return false;

        visited.Add(node);
        recursionStack.Add(node);

        if (adjacencyList.TryGetValue(node, out var neighbors))
        {
            foreach (var neighbor in neighbors)
            {
                if (HasCycle(neighbor, adjacencyList, visited, recursionStack))
                    return true;
            }
        }

        recursionStack.Remove(node);
        return false;
    }
}
