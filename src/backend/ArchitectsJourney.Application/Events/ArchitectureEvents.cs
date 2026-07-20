using ArchitectsJourney.Application.Common;
using ArchitectsJourney.Application.Events;

namespace ArchitectsJourney.Application.Events.Architecture;

public sealed record ArchitectureChangedEvent : DomainEvent
{
    public override string EventType => EventTypes.Architecture.Changed;
    public override EventCategory EventCategory => EventCategory.Architecture;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "RULE_ENGINE";

    public required string MutationType { get; init; }
    public required string TargetId { get; init; }
}

public sealed record ArchitectureInitializedEvent : DomainEvent
{
    public override string EventType => EventTypes.Architecture.Initialized;
    public override EventCategory EventCategory => EventCategory.Architecture;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.System;
    public override string ProducerId => "ARCHITECTURE_ENGINE";
    
    public required int InitialNodeCount { get; init; }
    public required int InitialEdgeCount { get; init; }
}

public sealed record ArchitectureNodeAddedEvent : DomainEvent
{
    public override string EventType => EventTypes.Architecture.NodeAdded;
    public override EventCategory EventCategory => EventCategory.Architecture;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "ARCHITECTURE_ENGINE";
    
    public required string NodeId { get; init; }
    public required string NodeType { get; init; }
}

public sealed record ArchitectureNodeRemovedEvent : DomainEvent
{
    public override string EventType => EventTypes.Architecture.NodeRemoved;
    public override EventCategory EventCategory => EventCategory.Architecture;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "ARCHITECTURE_ENGINE";
    
    public required string NodeId { get; init; }
}

public sealed record ArchitectureEdgeAddedEvent : DomainEvent
{
    public override string EventType => EventTypes.Architecture.EdgeAdded;
    public override EventCategory EventCategory => EventCategory.Architecture;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "ARCHITECTURE_ENGINE";
    
    public required string SourceNodeId { get; init; }
    public required string TargetNodeId { get; init; }
    public required string EdgeType { get; init; }
}

public sealed record ArchitectureEdgeRemovedEvent : DomainEvent
{
    public override string EventType => EventTypes.Architecture.EdgeRemoved;
    public override EventCategory EventCategory => EventCategory.Architecture;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.Domain;
    public override string ProducerId => "ARCHITECTURE_ENGINE";
    
    public required string SourceNodeId { get; init; }
    public required string TargetNodeId { get; init; }
    public required string EdgeType { get; init; }
}
