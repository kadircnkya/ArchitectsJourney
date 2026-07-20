namespace ArchitectsJourney.Application.Contracts;

public sealed class ArchitectureValidationOptions
{
    public bool AllowCycles { get; set; } = false;
    public bool AllowDanglingEdges { get; set; } = false;
    public bool RequireStrictTechAssignments { get; set; } = false;
}
