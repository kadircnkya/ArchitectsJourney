using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Application.DTOs.Mission;
using ArchitectsJourney.Domain.Aggregates;
using ArchitectsJourney.Domain.Entities;
using ArchitectsJourney.Domain.Enums;
using ArchitectsJourney.Domain.ValueObjects;

namespace ArchitectsJourney.Infrastructure.MissionLoading;

public sealed class MissionBuilder : IMissionBuilder
{
    public Mission Build(MissionDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new Mission(dto.Id)
        {
            Version = dto.Version,
            Title = dto.Title,
            Description = dto.Description,
            InitialMetrics = dto.InitialMetrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            InitialNodes = dto.InitialNodes.Select(BuildNode).ToList(),
            InitialEdges = dto.InitialEdges.Select(BuildEdge).ToList(),
            DecisionPoints = dto.DecisionPoints.Select(BuildDecisionPoint).ToList(),
            Rules = dto.Rules.Select(BuildRule).ToList()
        };
    }

    private MissionNodeDefinition BuildNode(MissionNodeDto dto) => new MissionNodeDefinition
    {
        NodeId = dto.Id,
        NodeType = dto.Type,
        Label = dto.Label,
        TechnologyId = dto.TechnologyId
    };

    private MissionEdgeDefinition BuildEdge(MissionEdgeDto dto) => new MissionEdgeDefinition
    {
        Source = dto.Source,
        Target = dto.Target,
        Type = Enum.Parse<EdgeType>(dto.Type, true),
        Communication = Enum.Parse<CommunicationType>(dto.Communication, true)
    };

    private DecisionPoint BuildDecisionPoint(DecisionPointDto dto) => new DecisionPoint(dto.Id)
    {
        Title = dto.Title,
        Phase = Enum.Parse<MissionPhase>(dto.Phase, true),
        Dialogue = dto.Dialogue,
        Options = dto.Options.Select(BuildDecisionOption).ToList()
    };

    private DecisionOption BuildDecisionOption(DecisionOptionDto dto) => new DecisionOption(dto.Id)
    {
        Label = dto.Label,
        Description = dto.Description,
        Condition = dto.Condition,
        MetricImpacts = dto.MetricImpacts.Select(BuildMetricImpact).ToList(),
        GraphMutations = dto.GraphMutations.ToList()
    };

    private MetricChangeEffect BuildMetricImpact(MetricChangeEffectDto dto) => new MetricChangeEffect
    {
        Metric = Enum.Parse<MetricType>(dto.Metric, true),
        Label = Enum.Parse<DeltaLabel>(dto.Label, true),
        Value = dto.Value
    };

    private MissionRuleDefinition BuildRule(MissionRuleDto dto) => new MissionRuleDefinition
    {
        RuleId = dto.Id,
        Trigger = dto.Trigger,
        Condition = dto.Condition,
        Effects = dto.Effects.ToList()
    };
}
