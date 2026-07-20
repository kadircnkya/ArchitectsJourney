namespace ArchitectsJourney.Application.Events;

/// <summary>
/// Canonical string identifiers for all 44 platform event types.
/// Used by the Event Bus for subscription routing and Schema Registry lookup.
/// Defined in Document 10, Section 4.
/// Format: SCREAMING_SNAKE_CASE as specified in Document 10, Section 17.2.
/// </summary>
public static class EventTypes
{
    // ──────────────────────────────────────────────────────────────────────────
    // Player Events (Document 10, Section 4.1)
    // ──────────────────────────────────────────────────────────────────────────
    public static class Player
    {
        public const string DecisionSubmitted = "PLAYER_DECISION_SUBMITTED";
        public const string QuestionAsked = "PLAYER_QUESTION_ASKED";
        public const string HandbookOpened = "PLAYER_HANDBOOK_OPENED";
        public const string SaveRequested = "PLAYER_SAVE_REQUESTED";
        public const string MissionRestartRequested = "PLAYER_MISSION_RESTART_REQUESTED";
        public const string SessionExitRequested = "PLAYER_SESSION_EXIT_REQUESTED";
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Rule Events (Document 10, Section 4.2)
    // ──────────────────────────────────────────────────────────────────────────
    public static class Rule
    {
        public const string DecisionProcessed = "DECISION_PROCESSED";
        public const string QuestionProcessed = "QUESTION_PROCESSED";
        public const string BusinessEventProcessed = "BUSINESS_EVENT_PROCESSED";
        public const string DerivedRuleTriggered = "DERIVED_RULE_TRIGGERED";
        public const string LearningObjectiveProgressed = "LEARNING_OBJECTIVE_PROGRESSED";
        public const string RuleExecutionAuditCreated = "RULE_EXECUTION_AUDIT_CREATED";
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Architecture Events (Document 10, Section 4.3)
    // ──────────────────────────────────────────────────────────────────────────
    public static class Architecture
    {
        public const string Initialized = "ARCHITECTURE_INITIALIZED";
        public const string NodeAdded = "ARCHITECTURE_NODE_ADDED";
        public const string NodeRemoved = "ARCHITECTURE_NODE_REMOVED";
        public const string EdgeAdded = "ARCHITECTURE_EDGE_ADDED";
        public const string EdgeRemoved = "ARCHITECTURE_EDGE_REMOVED";
        public const string NodeStressApplied = "NODE_STRESS_APPLIED";
        public const string NodeStressResolved = "NODE_STRESS_RESOLVED";
        public const string Changed = "ARCHITECTURE_CHANGED";
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Technology Events (Document 10, Section 4.4)
    // ──────────────────────────────────────────────────────────────────────────
    public static class Technology
    {
        public const string Discovered = "TECHNOLOGY_DISCOVERED";
        public const string Selected = "TECHNOLOGY_SELECTED";
        public const string HandbookEntryCreated = "HANDBOOK_ENTRY_CREATED";
        public const string HandbookEntryEnriched = "HANDBOOK_ENTRY_ENRICHED";
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Metric Events (Document 10, Section 4.5)
    // ──────────────────────────────────────────────────────────────────────────
    public static class Metric
    {
        public const string BaselineInitialized = "METRIC_BASELINE_INITIALIZED";
        public const string DeltaApplied = "METRIC_DELTA_APPLIED";
        public const string Updated = "METRICS_UPDATED";
        public const string ThresholdCrossed = "METRIC_THRESHOLD_CROSSED";
        public const string BoundsEnforced = "METRIC_BOUNDS_ENFORCED";
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Narrative Events (Document 10, Section 4.6)
    // ──────────────────────────────────────────────────────────────────────────
    public static class Narrative
    {
        public const string FeedbackQueued = "FEEDBACK_QUEUED";
        public const string FeedbackDelivered = "FEEDBACK_DELIVERED";
        public const string ClientStatementDelivered = "CLIENT_STATEMENT_DELIVERED";
        public const string QuestionRevealDelivered = "QUESTION_REVEAL_DELIVERED";
        public const string ReviewNarrativeDelivered = "REVIEW_NARRATIVE_DELIVERED";
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Review Events (Document 10, Section 4.7)
    // ──────────────────────────────────────────────────────────────────────────
    public static class Review
    {
        public const string GenerationRequested = "REVIEW_GENERATION_REQUESTED";
        public const string Generated = "REVIEW_GENERATED";
        public const string InsightUnlocked = "INSIGHT_UNLOCKED";
        public const string LessonsLearnedFinalized = "LESSONS_LEARNED_FINALIZED";
    }

    // ──────────────────────────────────────────────────────────────────────────
    // System Events (Document 10, Section 4.8)
    // ──────────────────────────────────────────────────────────────────────────
    public static class System
    {
        public const string SessionStarted = "SESSION_STARTED";
        public const string SessionRestored = "SESSION_RESTORED";
        public const string SessionEnded = "SESSION_ENDED";
        public const string MissionLoaded = "MISSION_LOADED";
        public const string MissionPhaseChanged = "MISSION_PHASE_CHANGED";
        public const string MissionCompleted = "MISSION_COMPLETED";
        public const string CheckpointRequired = "CHECKPOINT_REQUIRED";
        public const string CheckpointCreated = "CHECKPOINT_CREATED";
        public const string RollbackRequired = "ROLLBACK_REQUIRED";
        public const string RollbackCompleted = "ROLLBACK_COMPLETED";
        public const string SubsystemHealthDegraded = "SUBSYSTEM_HEALTH_DEGRADED";
    }
}
