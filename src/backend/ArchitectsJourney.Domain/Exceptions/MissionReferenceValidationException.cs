namespace ArchitectsJourney.Domain.Exceptions;

/// <summary>
/// Thrown when internal logical references do not point to existing entities within the parsed document.
/// Holds a collection of all accumulated invalid references.
/// </summary>
public sealed class MissionReferenceValidationException : MissionLoadException
{
    public IReadOnlyList<string> Errors { get; } = Array.Empty<string>();

    public MissionReferenceValidationException() { }
    public MissionReferenceValidationException(string message) : base(message) { }
    public MissionReferenceValidationException(string message, Exception innerException) : base(message, innerException) { }

    public MissionReferenceValidationException(string missionId, IReadOnlyList<string> errors) 
        : base($"Mission '{missionId}' failed reference validation with {errors?.Count ?? 0} errors:\n" + (errors != null ? string.Join("\n", errors) : string.Empty))
    {
        Errors = errors ?? Array.Empty<string>();
    }
}
