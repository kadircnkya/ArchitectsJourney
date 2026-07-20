using ArchitectsJourney.Domain.Common;

namespace ArchitectsJourney.Domain.Entities;

/// <summary>
/// Dynamic state of an architecture node during a playthrough.
/// </summary>
public sealed class ArchitectureNode : Entity<string>
{
    public ArchitectureNode(string id, string type, string label, string? technologyId) : base(id)
    {
        Type = type;
        Label = label;
        TechnologyId = technologyId;
    }

    public string Type { get; private set; }
    public string Label { get; private set; }
    public string? TechnologyId { get; private set; }

    public void UpdateTechnology(string? technologyId)
    {
        TechnologyId = technologyId;
    }
}
