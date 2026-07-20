using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Application.DTOs.Mission;
using ArchitectsJourney.Domain.Exceptions;

namespace ArchitectsJourney.Infrastructure.MissionLoading;

public sealed class MissionReferenceValidator : IMissionReferenceValidator
{
    private readonly ITechnologyCatalog _technologyCatalog;

    public MissionReferenceValidator(ITechnologyCatalog technologyCatalog)
    {
        _technologyCatalog = technologyCatalog;
    }

    public async Task ValidateAsync(string missionId, MissionDto missionDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(missionDto);

        var errors = new List<string>();

        // Gather all IDs to check for duplicates and internal references
        var nodeIds = new HashSet<string>();
        var edgeIds = new HashSet<string>();
        var ruleIds = new HashSet<string>();
        var decisionIds = new HashSet<string>();
        var optionIds = new HashSet<string>();

        // Nodes
        foreach (var node in missionDto.InitialNodes)
        {
            if (!nodeIds.Add(node.Id)) errors.Add($"Duplicate Node ID: {node.Id}");
            
            if (!string.IsNullOrEmpty(node.TechnologyId))
            {
                bool validTech = await _technologyCatalog.IsValidTechnologyAsync(node.TechnologyId, cancellationToken);
                if (!validTech)
                {
                    errors.Add($"Node {node.Id} references invalid technology: {node.TechnologyId}");
                }
            }
        }

        // Rules
        foreach (var rule in missionDto.Rules)
        {
            if (!ruleIds.Add(rule.Id)) errors.Add($"Duplicate Rule ID: {rule.Id}");
            
            // effects typically might reference rules/nodes depending on effect type, but for MVP let's just do basic checks
            // The prompt says "ruleId, nodeId, eventId, metricId, technologyId".
        }

        // Decisions
        foreach (var dp in missionDto.DecisionPoints)
        {
            if (!decisionIds.Add(dp.Id)) errors.Add($"Duplicate Decision Point ID: {dp.Id}");

            foreach (var opt in dp.Options)
            {
                if (!optionIds.Add(opt.Id)) errors.Add($"Duplicate Option ID: {opt.Id}");
                
                // metricImpacts check against initialMetrics or predefined enums
                foreach (var impact in opt.MetricImpacts)
                {
                    if (!missionDto.InitialMetrics.ContainsKey(impact.Metric) && 
                        !Enum.TryParse<ArchitectsJourney.Domain.Enums.MetricType>(impact.Metric, true, out _))
                    {
                        errors.Add($"Option {opt.Id} references unknown metric: {impact.Metric}");
                    }
                }

                // graphMutations check for rules or nodes
                foreach (var mutation in opt.GraphMutations)
                {
                    // Basic validation: just check if it's empty
                    if (string.IsNullOrWhiteSpace(mutation))
                    {
                        errors.Add($"Option {opt.Id} contains empty graph mutation.");
                    }
                }
            }
        }

        // Edges
        foreach (var edge in missionDto.InitialEdges)
        {
            if (!nodeIds.Contains(edge.Source)) errors.Add($"Edge references unknown source node: {edge.Source}");
            if (!nodeIds.Contains(edge.Target)) errors.Add($"Edge references unknown target node: {edge.Target}");
        }

        if (errors.Count > 0)
        {
            throw new MissionReferenceValidationException(missionId, errors);
        }
    }
}
