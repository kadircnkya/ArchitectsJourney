using ArchitectsJourney.Application.Common;
using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Application.Events;
using ArchitectsJourney.Application.Events.Player;
using ArchitectsJourney.Application.Events.System;
using ArchitectsJourney.Domain.Aggregates;
using ArchitectsJourney.Domain.Entities;
using ArchitectsJourney.Domain.Enums;
using ArchitectsJourney.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace ArchitectsJourney.Api.Controllers;

[ApiController]
[Route("api/sessions")]
public sealed class SessionController : ControllerBase
{
    private readonly IEventBus _eventBus;
    private readonly IMissionRepository _missionRepository;
    private readonly IPlaythroughRepository _playthroughRepository;
    private readonly IEnumerable<IGameSubsystem> _subsystems;

    public SessionController(
        IEventBus eventBus,
        IMissionRepository missionRepository,
        IPlaythroughRepository playthroughRepository,
        IEnumerable<IGameSubsystem> subsystems)
    {
        _eventBus = eventBus;
        _missionRepository = missionRepository;
        _playthroughRepository = playthroughRepository;
        _subsystems = subsystems;
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartSessionAsync(
        [FromQuery] string missionId,
        CancellationToken cancellationToken)
    {
        // 1. Seed dummy mission if repository is empty for demonstration purposes
        var existing = await _missionRepository.GetByIdAsync(missionId, cancellationToken);
        if (existing == null)
        {
            var seedMission = new Mission(missionId)
            {
                Title = "Cloud Migration Journey",
                Description = "Migrate legacy monolith to decoupled microservices.",
                Version = "1.0",
                InitialMetrics = new Dictionary<string, int>
                {
                    { "Performance", 80 },
                    { "Scalability", 50 },
                    { "Reliability", 60 },
                    { "Maintainability", 40 },
                    { "Complexity", 30 },
                    { "Cost", 90 },
                    { "Security", 75 }
                },
                InitialNodes = [],
                InitialEdges = [],
                DecisionPoints = [],
                Rules = [],
                Objectives = []
            };
            await _missionRepository.SaveAsync(seedMission, cancellationToken);
        }

        var context = new SessionContext
        {
            SessionId = Guid.NewGuid(),
            PlaythroughId = Guid.NewGuid(),
            MissionId = missionId,
            PlayerId = Guid.NewGuid()
        };

        // 2. Initialize all subsystems in order
        foreach (var subsystem in _subsystems)
        {
            var res = await subsystem.InitializeAsync(context, cancellationToken);
            if (!res.Success)
            {
                return BadRequest(Error.Conflict("Session.InitFailed", $"Subsystem {subsystem.SubsystemId} failed to init: {res.FailureReason}"));
            }
        }

        // 3. Publish SessionStarted Event
        var sessionStarted = new SessionStartedEvent
        {
            SessionId = context.SessionId,
            PlaythroughId = context.PlaythroughId,
            MissionId = context.MissionId,
            CorrelationId = Guid.NewGuid(),
            CausationId = null
        };
        await _eventBus.PublishAsync(sessionStarted, cancellationToken);

        return Ok(new { sessionId = context.SessionId, playthroughId = context.PlaythroughId });
    }

    [HttpPost("{sessionId}/decide")]
    public async Task<IActionResult> SubmitDecisionAsync(
        Guid sessionId,
        [FromQuery] Guid playthroughId,
        [FromQuery] string missionId,
        [FromQuery] string decisionId,
        [FromQuery] string optionId,
        CancellationToken cancellationToken)
    {
        var decisionEvent = new PlayerDecisionSubmittedEvent
        {
            SessionId = sessionId,
            PlaythroughId = playthroughId,
            MissionId = missionId,
            CorrelationId = Guid.NewGuid(),
            CausationId = null,
            DecisionPointId = decisionId,
            SelectedOptionId = optionId
        };

        var result = await _eventBus.PublishAsync(decisionEvent, cancellationToken);
        if (!result.Accepted)
        {
            return BadRequest(Error.Conflict("Decision.Rejected", result.RejectionReason ?? "Unknown validation error."));
        }

        return Ok(new { eventId = result.EventId, sequenceNumber = result.SequenceNumber });
    }

    [HttpPost("run-diagnostic")]
    public async Task<IActionResult> RunDiagnosticAsync(CancellationToken cancellationToken)
    {
        var logs = new List<string>();
        try
        {
            logs.Add("Seeding mock mission 'm1'...");
            var seedMission = new Mission("m1")
            {
                Title = "Diagnostic Mission",
                Description = "Diagnostic test run",
                Version = "1.0",
                InitialMetrics = new Dictionary<string, int>
                {
                    { "Performance", 80 },
                    { "Scalability", 50 },
                    { "Reliability", 60 },
                    { "Maintainability", 40 },
                    { "Complexity", 30 },
                    { "Cost", 90 },
                    { "Security", 75 }
                },
                InitialNodes = [],
                InitialEdges = [],
                Rules = [],
                Objectives = [],
                DecisionPoints = [
                    new DecisionPoint("dp_monolith")
                    {
                        Title = "Monolith Breakdown",
                        Dialogue = "Should we?",
                        Phase = MissionPhase.ClientMeeting,
                        Options = [
                            new DecisionOption("opt_microservices")
                            {
                                Label = "Microservices",
                                Description = "Separate",
                                Condition = null,
                                GraphMutations = [],
                                MetricImpacts = [
                                    new MetricChangeEffect
                                    {
                                        Metric = MetricType.Scalability,
                                        Label = DeltaLabel.MajorImprovement,
                                        Value = 20
                                    }
                                ]
                            }
                        ]
                    }
                ]
            };
            await _missionRepository.SaveAsync(seedMission, cancellationToken);

            var context = new SessionContext
            {
                SessionId = Guid.NewGuid(),
                PlaythroughId = Guid.NewGuid(),
                MissionId = "m1",
                PlayerId = Guid.NewGuid()
            };

            logs.Add("Initializing subsystems...");
            foreach (var sub in _subsystems)
            {
                var res = await sub.InitializeAsync(context, cancellationToken);
                logs.Add($"Subsystem {sub.SubsystemId} init success: {res.Success}");
            }

            var decisionEvent = new PlayerDecisionSubmittedEvent
            {
                SessionId = context.SessionId,
                PlaythroughId = context.PlaythroughId,
                MissionId = context.MissionId,
                CorrelationId = Guid.NewGuid(),
                CausationId = null,
                DecisionPointId = "dp_monolith",
                SelectedOptionId = "opt_microservices"
            };

            logs.Add("Publishing decision event...");
            var publishResult = await _eventBus.PublishAsync(decisionEvent, cancellationToken);
            logs.Add($"Publish accepted: {publishResult.Accepted}");

            logs.Add("Waiting for execution queue to drain...");
            await Task.Delay(250, cancellationToken);

            logs.Add("Retrieving playthrough state...");
            var playthrough = await _playthroughRepository.GetByIdAsync(context.PlaythroughId, cancellationToken);
            if (playthrough == null)
            {
                return Ok(new { success = false, logs, error = "Playthrough not persisted." });
            }

            playthrough.Metrics.TryGetValue(MetricType.Scalability, out int scalability);
            logs.Add($"Playthrough Scalability: {scalability} (Expected: 70)");

            return Ok(new { success = scalability == 70, logs });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, logs, error = ex.ToString() });
        }
    }
}

public sealed record SessionStartedEvent : DomainEvent
{
    public override string EventType => EventTypes.System.SessionStarted;
    public override EventCategory EventCategory => EventCategory.System;
    public override string SchemaVersion => "1.0";
    public override EventPriority Priority => EventPriority.System;
    public override string ProducerId => "GAME_ENGINE";
}
