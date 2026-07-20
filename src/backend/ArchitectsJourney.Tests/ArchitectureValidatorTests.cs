using System.Collections.Generic;
using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Domain.Entities;
using ArchitectsJourney.Domain.ValueObjects;
using ArchitectsJourney.Infrastructure.Architecture;
using ArchitectsJourney.Domain.Enums;
using Xunit;

namespace ArchitectsJourney.Tests;

public class ArchitectureValidatorTests
{
    [Fact]
    public void ValidateEdgeAddition_WithCycleDetectionEnabled_DetectsCycle()
    {
        // Arrange
        var options = new ArchitectureValidationOptions { AllowCycles = false };
        var validator = new ArchitectureValidator(options);

        var nodes = new List<ArchitectureNode>
        {
            new("A", "Service", "Service A", null),
            new("B", "Service", "Service B", null),
            new("C", "Service", "Service C", null)
        };

        var edges = new List<ArchitectureEdge>
        {
            new("A", "B", EdgeType.Dependency, CommunicationType.Synchronous),
            new("B", "C", EdgeType.Dependency, CommunicationType.Synchronous)
        };

        var edgeToAdd = new ArchitectureEdge("C", "A", EdgeType.Dependency, CommunicationType.Synchronous);

        // Act
        var result = validator.ValidateEdgeAddition(nodes, edges, edgeToAdd);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("cycle", result.ErrorMessage, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateEdgeAddition_WithCycleDetectionDisabled_AllowsCycle()
    {
        // Arrange
        var options = new ArchitectureValidationOptions { AllowCycles = true };
        var validator = new ArchitectureValidator(options);

        var nodes = new List<ArchitectureNode>
        {
            new("A", "Service", "Service A", null),
            new("B", "Service", "Service B", null)
        };

        var edges = new List<ArchitectureEdge>
        {
            new("A", "B", EdgeType.Dependency, CommunicationType.Synchronous)
        };

        var edgeToAdd = new ArchitectureEdge("B", "A", EdgeType.Dependency, CommunicationType.Synchronous);

        // Act
        var result = validator.ValidateEdgeAddition(nodes, edges, edgeToAdd);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateNodeRemoval_WithDanglingEdgesDisabled_PreventsRemoval()
    {
        // Arrange
        var options = new ArchitectureValidationOptions { AllowDanglingEdges = false };
        var validator = new ArchitectureValidator(options);

        var nodes = new List<ArchitectureNode>
        {
            new("A", "Service", "Service A", null),
            new("B", "Service", "Service B", null)
        };

        var edges = new List<ArchitectureEdge>
        {
            new("A", "B", EdgeType.Dependency, CommunicationType.Synchronous)
        };

        // Act
        var result = validator.ValidateNodeRemoval(nodes, edges, "A");

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("connected edges", result.ErrorMessage, System.StringComparison.OrdinalIgnoreCase);
    }
}
