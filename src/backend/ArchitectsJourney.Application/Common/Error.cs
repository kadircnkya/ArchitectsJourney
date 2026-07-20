namespace ArchitectsJourney.Application.Common;

/// <summary>
/// Represents a domain or application error with a machine-readable code and human-readable description.
/// Used as the failure case of <see cref="Result{T}"/>.
/// </summary>
public sealed record Error(string Code, string Description)
{
    // ──────────────────────────────────────────────────────────────────────────
    // Common platform errors
    // ──────────────────────────────────────────────────────────────────────────
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error NotFound(string resource, string id) =>
        new($"{resource}.NotFound", $"{resource} with id '{id}' was not found.");

    public static Error Invalid(string field, string reason) =>
        new($"Validation.{field}", reason);

    public static Error Conflict(string code, string description) =>
        new(code, description);

    public static Error Unexpected(string code, string description) =>
        new(code, description);

    // ──────────────────────────────────────────────────────────────────────────
    // Session errors
    // ──────────────────────────────────────────────────────────────────────────
    public static readonly Error SessionLocked =
        new("Session.Locked", "An action cycle is currently in progress. Wait for it to complete.");

    public static readonly Error SessionNotFound =
        new("Session.NotFound", "No active session was found for the given session identifier.");

    // ──────────────────────────────────────────────────────────────────────────
    // Mission errors
    // ──────────────────────────────────────────────────────────────────────────
    public static Error MissionNotFound(string missionId) =>
        new("Mission.NotFound", $"Mission '{missionId}' does not exist or is not available.");

    public static Error MissionValidationFailed(string reason) =>
        new("Mission.ValidationFailed", reason);

    // ──────────────────────────────────────────────────────────────────────────
    // Decision errors
    // ──────────────────────────────────────────────────────────────────────────
    public static Error DecisionPointNotFound(string id) =>
        new("Decision.PointNotFound", $"Decision point '{id}' was not found in the current mission.");

    public static Error DecisionPointAlreadyResolved(string id) =>
        new("Decision.AlreadyResolved", $"Decision point '{id}' has already been resolved in this session.");

    public static Error OptionNotAvailable(string optionId) =>
        new("Decision.OptionNotAvailable", $"Option '{optionId}' is not available. Its condition is not satisfied.");

    public static Error OptionNotFound(string optionId) =>
        new("Decision.OptionNotFound", $"Option '{optionId}' does not exist in this decision point.");

    public override string ToString() => $"[{Code}] {Description}";
}
