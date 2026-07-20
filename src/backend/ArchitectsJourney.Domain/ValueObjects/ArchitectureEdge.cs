using System.Collections.Generic;
using ArchitectsJourney.Domain.Common;
using ArchitectsJourney.Domain.Enums;

namespace ArchitectsJourney.Domain.ValueObjects;

/// <summary>
/// Dynamic state of an architecture edge during a playthrough.
/// </summary>
public sealed class ArchitectureEdge : ValueObject
{
    public ArchitectureEdge(string source, string target, EdgeType type, CommunicationType communication)
    {
        Source = source;
        Target = target;
        Type = type;
        Communication = communication;
    }

    public string Source { get; }
    public string Target { get; }
    public EdgeType Type { get; }
    public CommunicationType Communication { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Source;
        yield return Target;
        yield return Type;
        yield return Communication;
    }
}
