namespace ArchitectsJourney.Domain.Exceptions;

/// <summary>
/// Thrown when the requested mission file does not exist.
/// </summary>
public sealed class MissionNotFoundException : MissionLoadException
{
    public MissionNotFoundException() { }
    public MissionNotFoundException(string message) : base(message) { }
    public MissionNotFoundException(string message, Exception innerException) : base(message, innerException) { }

    public static MissionNotFoundException ForMissionId(string missionId) 
        => new MissionNotFoundException($"Mission '{missionId}' could not be found.");
}
