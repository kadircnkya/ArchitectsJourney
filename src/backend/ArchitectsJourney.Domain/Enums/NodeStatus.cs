namespace ArchitectsJourney.Domain.Enums;

/// <summary>
/// Node stress states in the architecture graph.
/// Defined in Document 05 (Domain Model) and Document 09 (Architecture Model).
/// </summary>
public enum NodeStatus
{
    Active,
    Stressed,
    Degraded
}
