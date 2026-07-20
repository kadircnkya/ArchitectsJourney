namespace ArchitectsJourney.Domain.Exceptions;

/// <summary>
/// Thrown when the mission file version is not supported by the current loader.
/// </summary>
public sealed class MissionVersionNotSupportedException : MissionLoadException
{
    public MissionVersionNotSupportedException() { }
    public MissionVersionNotSupportedException(string message) : base(message) { }
    public MissionVersionNotSupportedException(string message, Exception innerException) : base(message, innerException) { }

    public static MissionVersionNotSupportedException ForVersion(string missionId, string version) 
        => new MissionVersionNotSupportedException($"Mission '{missionId}' has unsupported version '{version}'.");
}
