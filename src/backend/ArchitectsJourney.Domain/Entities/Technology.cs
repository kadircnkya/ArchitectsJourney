using System.Collections.Generic;
using ArchitectsJourney.Domain.Common;

namespace ArchitectsJourney.Domain.Entities;

/// <summary>
/// Domain entity representing a technology blueprint in the catalog.
/// As per Phase 7 constraints, this is a catalog definition and not an aggregate root.
/// </summary>
public sealed class Technology : Entity<string>
{
    private readonly List<string> _prerequisites = [];
    private readonly List<string> _conflicts = [];

    public Technology(string id, string name, string category) : base(id)
    {
        Name = name;
        Category = category;
    }

    public string Name { get; private set; }
    public string Category { get; private set; }
    
    public IReadOnlyList<string> Prerequisites => _prerequisites.AsReadOnly();
    public IReadOnlyList<string> Conflicts => _conflicts.AsReadOnly();

    public void AddPrerequisite(string technologyId)
    {
        if (!_prerequisites.Contains(technologyId))
        {
            _prerequisites.Add(technologyId);
        }
    }

    public void AddConflict(string technologyId)
    {
        if (!_conflicts.Contains(technologyId))
        {
            _conflicts.Add(technologyId);
        }
    }
}
