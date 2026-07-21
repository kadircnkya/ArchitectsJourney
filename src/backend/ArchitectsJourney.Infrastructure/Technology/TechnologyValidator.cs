using System.Collections.Generic;
using System.Linq;
using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Domain.Entities;

namespace ArchitectsJourney.Infrastructure.Technology;

public sealed class TechnologyValidator : ITechnologyValidator
{
    public TechnologyValidationResult ValidateTechnologyUsage(
        IReadOnlyList<ArchitectureNode> currentNodes,
        IReadOnlySet<string> discoveredTechnologies,
        string targetNodeId,
        Domain.Entities.Technology requestedTechnology)
    {
        System.ArgumentNullException.ThrowIfNull(currentNodes);
        System.ArgumentNullException.ThrowIfNull(discoveredTechnologies);
        System.ArgumentNullException.ThrowIfNull(requestedTechnology);

        // 1. Check if the technology has been discovered by the player
        if (!discoveredTechnologies.Contains(requestedTechnology.Id))
        {
            return TechnologyValidationResult.Unavailable($"Technology '{requestedTechnology.Id}' has not been discovered yet.");
        }

        // Gather all currently active technologies across the architecture
        // Excluding the target node to simulate replacing its technology without self-conflict
        var activeTechnologies = currentNodes
            .Where(n => n.Id != targetNodeId && !string.IsNullOrEmpty(n.TechnologyId))
            .Select(n => n.TechnologyId!)
            .ToHashSet();

        // 2. Check for conflicts
        foreach (var conflictId in requestedTechnology.Conflicts)
        {
            if (activeTechnologies.Contains(conflictId))
            {
                return TechnologyValidationResult.Conflict(
                    $"Technology '{requestedTechnology.Id}' conflicts with already active technology '{conflictId}'.",
                    conflictId);
            }
        }

        // 3. Check for prerequisites
        foreach (var prereqId in requestedTechnology.Prerequisites)
        {
            if (!activeTechnologies.Contains(prereqId))
            {
                return TechnologyValidationResult.Conflict(
                    $"Technology '{requestedTechnology.Id}' requires prerequisite technology '{prereqId}' to be present in the architecture.",
                    prereqId);
            }
        }

        return TechnologyValidationResult.Success();
    }
}
