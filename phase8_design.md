# Phase 8: Mission Engine Design

## Overview
Phase 8 introduces the **Mission Engine**. This engine acts as a stateless orchestrator that tracks mission objectives dynamically based on system state changes. 
Crucially, it is non-breaking: `GameEngine` retains its current responsibilities and tests, while `MissionEngine` layers on top to evaluate complex objectives.

## Domain Layer

### Entities & Value Objects
- **New Entity**: `MissionObjective` (tracks the status of individual objectives within a mission).
- **New Value Object**: `ObjectiveCondition` (defines the criteria for an objective, e.g., a specific metric threshold or architecture node presence).
- **Aggregate Changes**: `Playthrough` will track completed objectives.
- **Domain Events**: 
  - `MissionObjectiveCompletedEvent`
  - `MissionObjectiveFailedEvent`

### State Ownership
The `Playthrough` aggregate root continues to own all mission state mutations (i.e., marking an objective as completed). The `MissionEngine` acts only as a stateless orchestrator.

## Application Layer

### Interfaces & Contracts
- The `MissionEngine` will simply implement the existing `IGameSubsystem` interface. No new `IMissionEngine` abstraction will be introduced unless strictly required by the DI container pattern.

### Services
- The engine will utilize `IMissionLoader` to fetch mission definitions and objective criteria when evaluation is triggered.

### Dependencies
- `IEventBus` for event subscription and publishing.
- `IPlaythroughRepository` to load and save `Playthrough` state.
- `IMissionLoader` to retrieve static mission configuration.
- `IEventIdempotencyTracker` to prevent duplicate evaluations.

## Infrastructure Layer

### Persistence Changes
- Updates to EF Core mappings or Playthrough persistence logic to serialize the new `MissionObjective` states within the `Playthrough` snapshot.

### Configuration Requirements
- Register `MissionEngine` as a hosted service or scoped dependency implementing `IGameSubsystem`.
- Event Bus topic bindings for new subscriptions.

## Engine Layer

### Responsibilities
- Track and evaluate mission progression dynamically via objectives.
- Stateless Orchestration flow:
  1. Load Playthrough
  2. Evaluate Objectives
  3. Mutate Aggregate
  4. Save
  5. Publish Mission events

### Event Subscriptions
- `Rule.DecisionProcessed`
- `Metric.ThresholdCrossed` / `Metric.Updated`
- `Architecture.Changed`

### Event Publishing
- `MissionObjectiveCompletedEvent`
- `MissionObjectiveFailedEvent`
- `System.MissionCompleted` (when all objectives are met)

### Processing Pipeline
1. Receive Domain Event (e.g., `MetricsUpdated`).
2. Idempotency Check.
3. Load `Playthrough`.
4. Fetch Mission details via `IMissionLoader`.
5. Evaluate `MissionObjective` conditions against current `Playthrough` metrics/architecture.
6. Mutate `Playthrough` aggregate (mark objectives complete).
7. Save `Playthrough`.
8. Publish `MissionObjectiveCompletedEvent` or `MissionCompleted` events.

## Integration Design

### Interaction with Subsystems
- **GameEngine**: Retains its existing behavior and hardcoded sequential phase transitions. The `MissionEngine` operates in parallel, subscribing to existing events without altering `GameEngine`'s logic.
- **RuleEngine / MetricEngine / ArchitectureEngine / TechnologyHandbookEngine**: These engines produce events that the `MissionEngine` listens to. They remain oblivious to mission progression logic.

### Event Ordering Decisions
`MissionEngine` acts as an observer. It listens to downstream events (like `MetricsUpdated` or `ArchitectureChanged`) to evaluate objectives. It ensures that objective evaluation only happens *after* the core state has settled.

## Testing Strategy

### Unit Tests
- New `MissionEngineTests.cs` covering:
  - Objective evaluation logic (metric and architecture conditions).
  - Proper state mutation calls on the `Playthrough` aggregate.
- **GameEngine unit tests remain completely unchanged and valid.**

### Integration Tests
- Verify interaction between `GameEngine` and `MissionEngine` without replacing existing tests.
- End-to-end flow: Decision -> Rule -> Metric -> MissionEngine evaluates objective -> Events published.

### Architecture Constraints
- Ensure idempotency and statelessness.
- Ensure all mission state ownership remains inside the `Playthrough` aggregate.
