namespace ArchitectsJourney.Domain.Enums;

/// <summary>
/// Architecture edge types defined in Document 08 (Mission JSON Specification, Section 9).
/// </summary>
public enum EdgeType
{
    RequestResponse,
    Event,
    DataStream,
    Dependency
}
