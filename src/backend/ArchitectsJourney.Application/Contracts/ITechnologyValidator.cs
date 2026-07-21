using System.Collections.Generic;

namespace ArchitectsJourney.Application.Contracts;

public sealed record TechnologyValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ConflictingTechnologyId { get; init; }

    public static TechnologyValidationResult Success() => new() { IsValid = true };
    public static TechnologyValidationResult Conflict(string message, string conflictingId) => new() { IsValid = false, ErrorMessage = message, ConflictingTechnologyId = conflictingId };
    public static TechnologyValidationResult Unavailable(string message) => new() { IsValid = false, ErrorMessage = message };
}

public interface ITechnologyValidator
{
    TechnologyValidationResult ValidateTechnologyUsage(
        IReadOnlyList<Domain.Entities.ArchitectureNode> currentNodes, 
        IReadOnlySet<string> discoveredTechnologies, 
        string targetNodeId, 
        Domain.Entities.Technology requestedTechnology);
}
