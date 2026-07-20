using ArchitectsJourney.Domain.Common;
using ArchitectsJourney.Domain.Enums;

namespace ArchitectsJourney.Domain.Aggregates;

public sealed record ArchitectureNodeSnapshot
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public required string Label { get; init; }
    public string? TechnologyId { get; init; }
}

public sealed record ArchitectureEdgeSnapshot
{
    public required string Source { get; init; }
    public required string Target { get; init; }
    public required string Type { get; init; }
    public required string Communication { get; init; }
}

public sealed record PlaythroughStateSnapshot
{
    public required Guid PlaythroughId { get; init; }
    public required string MissionId { get; init; }
    public required string CurrentPhase { get; init; }
    public required IReadOnlyList<string> ResolvedDecisions { get; init; }
    public required IReadOnlyList<string> UnlockedTechnologies { get; init; }
    public required IReadOnlyDictionary<string, int> CurrentMetrics { get; init; }
    public required IReadOnlyList<ArchitectsJourney.Domain.ValueObjects.MetricHistoryEntry> MetricHistory { get; init; }
    public required IReadOnlyList<ArchitectureNodeSnapshot> Nodes { get; init; }
    public required IReadOnlyList<ArchitectureEdgeSnapshot> Edges { get; init; }
}

/// <summary>
/// Playthrough aggregate root representing active dynamic simulation state.
/// Tracks current phase, resolved decisions, unlocked tech catalog, and metric mutations.
/// Enforces domain-level constraints defined in Document 05.
/// </summary>
public sealed class Playthrough : AggregateRoot<Guid>
{
    private readonly List<string> _resolvedDecisions = [];
    private readonly List<string> _unlockedTechnologies = [];
    private readonly Dictionary<MetricType, int> _metrics = [];
    private readonly List<ArchitectsJourney.Domain.ValueObjects.MetricHistoryEntry> _metricHistory = [];
    private readonly List<ArchitectsJourney.Domain.Entities.ArchitectureNode> _nodes = [];
    private readonly List<ArchitectsJourney.Domain.ValueObjects.ArchitectureEdge> _edges = [];

    public Playthrough(Guid id, string missionId) : base(id)
    {
        MissionId = missionId;
        CurrentPhase = MissionPhase.ClientMeeting;
    }

    public string MissionId { get; private set; }
    public MissionPhase CurrentPhase { get; private set; }
    public IReadOnlyList<string> ResolvedDecisions => _resolvedDecisions.AsReadOnly();
    public IReadOnlyList<string> UnlockedTechnologies => _unlockedTechnologies.AsReadOnly();
    public IReadOnlyDictionary<MetricType, int> Metrics => _metrics.AsReadOnly();
    public IReadOnlyList<ArchitectsJourney.Domain.ValueObjects.MetricHistoryEntry> MetricHistory => _metricHistory.AsReadOnly();
    public IReadOnlyList<ArchitectsJourney.Domain.Entities.ArchitectureNode> Nodes => _nodes.AsReadOnly();
    public IReadOnlyList<ArchitectsJourney.Domain.ValueObjects.ArchitectureEdge> Edges => _edges.AsReadOnly();

    public void InitializeMetrics(IReadOnlyDictionary<MetricType, int> initialMetrics)
    {
        ArgumentNullException.ThrowIfNull(initialMetrics);
        _metrics.Clear();
        foreach (var kvp in initialMetrics)
        {
            _metrics[kvp.Key] = kvp.Value;
        }
    }

    public void SetMetricValue(MetricType metric, int value)
    {
        _metrics[metric] = value;
    }

    public void AppendMetricHistory(ArchitectsJourney.Domain.ValueObjects.MetricHistoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _metricHistory.Add(entry);
    }

    public void UnlockTechnology(string technologyId)
    {
        if (!_unlockedTechnologies.Contains(technologyId))
        {
            _unlockedTechnologies.Add(technologyId);
        }
    }

    public void ResolveDecision(string decisionPointId, string optionId)
    {
        if (!_resolvedDecisions.Contains(decisionPointId))
        {
            _resolvedDecisions.Add(decisionPointId);
        }
    }

    public void TransitionToPhase(MissionPhase nextPhase)
    {
        // Enforce sequential phase progress
        if (nextPhase > CurrentPhase)
        {
            CurrentPhase = nextPhase;
        }
    }

    public void InitializeArchitecture(IReadOnlyList<ArchitectsJourney.Domain.ValueObjects.MissionNodeDefinition> initialNodes, IReadOnlyList<ArchitectsJourney.Domain.ValueObjects.MissionEdgeDefinition> initialEdges)
    {
        ArgumentNullException.ThrowIfNull(initialNodes);
        ArgumentNullException.ThrowIfNull(initialEdges);

        _nodes.Clear();
        foreach (var nodeDef in initialNodes)
        {
            _nodes.Add(new ArchitectsJourney.Domain.Entities.ArchitectureNode(nodeDef.NodeId, nodeDef.NodeType, nodeDef.Label, nodeDef.TechnologyId));
        }

        _edges.Clear();
        foreach (var edgeDef in initialEdges)
        {
            _edges.Add(new ArchitectsJourney.Domain.ValueObjects.ArchitectureEdge(edgeDef.Source, edgeDef.Target, edgeDef.Type, edgeDef.Communication));
        }
    }

    public void AddNode(ArchitectsJourney.Domain.Entities.ArchitectureNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!_nodes.Any(n => n.Id == node.Id))
        {
            _nodes.Add(node);
        }
    }

    public void RemoveNode(string nodeId)
    {
        _nodes.RemoveAll(n => n.Id == nodeId);
        // Cascading edge removal handled implicitly or via rule engine orchestration, 
        // but aggregate could clean up dangling edges here if domain rules mandate it.
        _edges.RemoveAll(e => e.Source == nodeId || e.Target == nodeId);
    }

    public void AddEdge(ArchitectsJourney.Domain.ValueObjects.ArchitectureEdge edge)
    {
        ArgumentNullException.ThrowIfNull(edge);
        if (!_edges.Any(e => e.Source == edge.Source && e.Target == edge.Target && e.Type == edge.Type))
        {
            _edges.Add(edge);
        }
    }

    public void RemoveEdge(string source, string target, EdgeType type)
    {
        _edges.RemoveAll(e => e.Source == source && e.Target == target && e.Type == type);
    }

    public void UpdateNodeTechnology(string nodeId, string? technologyId)
    {
        var node = _nodes.FirstOrDefault(n => n.Id == nodeId);
        node?.UpdateTechnology(technologyId);
    }

    public PlaythroughStateSnapshot TakeSnapshot() => new()
    {
        PlaythroughId = Id,
        MissionId = MissionId,
        CurrentPhase = CurrentPhase.ToString(),
        ResolvedDecisions = [.. _resolvedDecisions],
        UnlockedTechnologies = [.. _unlockedTechnologies],
        CurrentMetrics = _metrics.ToDictionary(k => k.Key.ToString(), v => v.Value),
        MetricHistory = [.. _metricHistory],
        Nodes = _nodes.Select(n => new ArchitectureNodeSnapshot
        {
            Id = n.Id,
            Type = n.Type,
            Label = n.Label,
            TechnologyId = n.TechnologyId
        }).ToList(),
        Edges = _edges.Select(e => new ArchitectureEdgeSnapshot
        {
            Source = e.Source,
            Target = e.Target,
            Type = e.Type.ToString(),
            Communication = e.Communication.ToString()
        }).ToList()
    };

    public void Restore(PlaythroughStateSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        MissionId = snapshot.MissionId;
        CurrentPhase = Enum.Parse<MissionPhase>(snapshot.CurrentPhase);
        
        _resolvedDecisions.Clear();
        _resolvedDecisions.AddRange(snapshot.ResolvedDecisions);

        _unlockedTechnologies.Clear();
        _unlockedTechnologies.AddRange(snapshot.UnlockedTechnologies);

        _metrics.Clear();
        foreach (var kvp in snapshot.CurrentMetrics)
        {
            if (Enum.TryParse<MetricType>(kvp.Key, out var metric))
            {
                _metrics[metric] = kvp.Value;
            }
        }

        _metricHistory.Clear();
        _metricHistory.AddRange(snapshot.MetricHistory);

        _nodes.Clear();
        if (snapshot.Nodes != null)
        {
            foreach (var nodeSnap in snapshot.Nodes)
            {
                _nodes.Add(new ArchitectsJourney.Domain.Entities.ArchitectureNode(nodeSnap.Id, nodeSnap.Type, nodeSnap.Label, nodeSnap.TechnologyId));
            }
        }

        _edges.Clear();
        if (snapshot.Edges != null)
        {
            foreach (var edgeSnap in snapshot.Edges)
            {
                if (Enum.TryParse<EdgeType>(edgeSnap.Type, out var edgeType) && 
                    Enum.TryParse<CommunicationType>(edgeSnap.Communication, out var commType))
                {
                    _edges.Add(new ArchitectsJourney.Domain.ValueObjects.ArchitectureEdge(edgeSnap.Source, edgeSnap.Target, edgeType, commType));
                }
            }
        }
    }
}
