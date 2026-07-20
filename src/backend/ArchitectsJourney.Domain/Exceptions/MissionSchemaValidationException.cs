namespace ArchitectsJourney.Domain.Exceptions;

/// <summary>
/// Thrown when the raw JSON fails validation against the schema.
/// Holds a collection of all accumulated validation errors.
/// </summary>
public sealed class MissionSchemaValidationException : MissionLoadException
{
    public IReadOnlyList<string> Errors { get; } = Array.Empty<string>();

    public MissionSchemaValidationException() { }
    public MissionSchemaValidationException(string message) : base(message) { }
    public MissionSchemaValidationException(string message, Exception innerException) : base(message, innerException) { }

    public MissionSchemaValidationException(string missionId, IReadOnlyList<string> errors) 
        : base($"Mission '{missionId}' failed schema validation with {errors?.Count ?? 0} errors:\n" + (errors != null ? string.Join("\n", errors) : string.Empty))
    {
        Errors = errors ?? Array.Empty<string>();
    }
}
