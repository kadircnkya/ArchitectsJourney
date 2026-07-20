namespace ArchitectsJourney.Domain.Exceptions;

/// <summary>
/// Thrown when a technologyId referenced in the mission file is rejected by the Technology Catalog.
/// </summary>
public sealed class InvalidTechnologyException : MissionLoadException
{
    public InvalidTechnologyException() { }
    public InvalidTechnologyException(string message) : base(message) { }
    public InvalidTechnologyException(string message, Exception innerException) : base(message, innerException) { }

    public static InvalidTechnologyException ForTechnology(string missionId, string technologyId) 
        => new InvalidTechnologyException($"Mission '{missionId}' references an invalid technology '{technologyId}'.");
}
