namespace ArchitectsJourney.Domain.Exceptions;

/// <summary>
/// Base exception for all mission loading pipeline errors.
/// </summary>
public abstract class MissionLoadException : Exception
{
    protected MissionLoadException() { }
    protected MissionLoadException(string message) : base(message) { }
    protected MissionLoadException(string message, Exception innerException) : base(message, innerException) { }
}
