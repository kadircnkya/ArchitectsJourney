using System.Collections.Generic;
using ArchitectsJourney.Domain.Entities;
using ArchitectsJourney.Infrastructure.Technology;
using Xunit;

namespace ArchitectsJourney.Tests;

public class TechnologyValidatorTests
{
    [Fact]
    public void ValidateTechnologyUsage_NotDiscovered_ReturnsUnavailable()
    {
        // Arrange
        var validator = new TechnologyValidator();
        var nodes = new List<ArchitectureNode>();
        var discovered = new HashSet<string> { "tech_a" };
        var requested = new Technology("tech_b", "Tech B", "Cat");

        // Act
        var result = validator.ValidateTechnologyUsage(nodes, discovered, "Node1", requested);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("not been discovered", result.ErrorMessage, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateTechnologyUsage_Conflict_ReturnsConflict()
    {
        // Arrange
        var validator = new TechnologyValidator();
        var nodes = new List<ArchitectureNode>
        {
            new("Node2", "Service", "Service A", "tech_a")
        };
        var discovered = new HashSet<string> { "tech_b" };
        var requested = new Technology("tech_b", "Tech B", "Cat");
        requested.AddConflict("tech_a");

        // Act
        var result = validator.ValidateTechnologyUsage(nodes, discovered, "Node1", requested);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("tech_a", result.ConflictingTechnologyId);
        Assert.Contains("conflicts", result.ErrorMessage, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateTechnologyUsage_MissingPrerequisite_ReturnsConflict()
    {
        // Arrange
        var validator = new TechnologyValidator();
        var nodes = new List<ArchitectureNode>
        {
            new("Node2", "Service", "Service A", "tech_c") // Missing tech_a
        };
        var discovered = new HashSet<string> { "tech_b" };
        var requested = new Technology("tech_b", "Tech B", "Cat");
        requested.AddPrerequisite("tech_a");

        // Act
        var result = validator.ValidateTechnologyUsage(nodes, discovered, "Node1", requested);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("tech_a", result.ConflictingTechnologyId);
        Assert.Contains("prerequisite", result.ErrorMessage, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateTechnologyUsage_Valid_ReturnsSuccess()
    {
        // Arrange
        var validator = new TechnologyValidator();
        var nodes = new List<ArchitectureNode>
        {
            new("Node2", "Service", "Service A", "tech_a")
        };
        var discovered = new HashSet<string> { "tech_b" };
        var requested = new Technology("tech_b", "Tech B", "Cat");
        requested.AddPrerequisite("tech_a");

        // Act
        var result = validator.ValidateTechnologyUsage(nodes, discovered, "Node1", requested);

        // Assert
        Assert.True(result.IsValid);
    }
}
