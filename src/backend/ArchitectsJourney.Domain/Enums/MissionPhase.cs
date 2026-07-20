namespace ArchitectsJourney.Domain.Enums;

/// <summary>
/// The lifecycle phases of an active mission playthrough.
/// Defined in Document 05 (Domain Model) and Document 09 (Game Engine).
/// </summary>
public enum MissionPhase
{
    ClientMeeting,
    RequirementGathering,
    ArchitectureDecisions,
    BusinessEvolution,
    ArchitectureEvolution,
    MissionComplete
}
