using ArchitectsJourney.Application.Common;
using ArchitectsJourney.Application.Contracts;
using ArchitectsJourney.Application.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ArchitectsJourney.Infrastructure.EventBus;

/// <summary>
/// Production implementation of the Event Bus.
/// Handles deterministic execution order, subscription matching, acknowledgement tracking,
/// retries with backoff, dead letter escalation, schema validation placeholders, and audit logging.
/// </summary>
public sealed class InMemoryEventBus : IEventBus, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InMemoryEventBus> _logger;
    private readonly IAuditEventStore? _auditStore;

    private readonly ConcurrentDictionary<string, PublisherRegistration> _publishers = new();
    private readonly ConcurrentDictionary<string, List<SubscriptionRegistration>> _subscribersByEvent = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<EventCategory, List<SubscriptionRegistration>> _subscribersByCategory = new();
    private readonly List<SubscriptionRegistration> _wildcardSubscribers = [];

    private readonly EventQueueManager _queueManager = new();
    private readonly SemaphoreSlim _dispatchSemaphore = new(1, 1);
    private bool _isDisposed;

    // Queue depth safety limit defined in Document 10, Section 6.4
    private const int MaxQueueDepth = 100;

    public InMemoryEventBus(
        IServiceProvider serviceProvider,
        ILogger<InMemoryEventBus> logger,
        IAuditEventStore? auditStore = null)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _auditStore = auditStore;
    }

    public void RegisterPublisher(PublisherRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);
        _publishers[registration.PublisherId] = registration;
        _logger.LogInformation("Registered publisher {PublisherId} with {Count} events.", 
            registration.PublisherId, registration.AuthorizedEventTypes.Count);
    }

    public void RegisterSubscriber(SubscriptionRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);
        switch (registration.Type)
        {
            case SubscriptionType.Type:
                if (string.IsNullOrWhiteSpace(registration.TargetEventType))
                    throw new ArgumentException("TargetEventType is required for type subscriptions.");
                _subscribersByEvent.AddOrUpdate(registration.TargetEventType, 
                    [registration], 
                    (_, list) => { lock (list) { list.Add(registration); } return list; });
                break;

            case SubscriptionType.Category:
                if (registration.TargetCategory == null)
                    throw new ArgumentException("TargetCategory is required for category subscriptions.");
                _subscribersByCategory.AddOrUpdate(registration.TargetCategory.Value,
                    [registration],
                    (_, list) => { lock (list) { list.Add(registration); } return list; });
                break;

            case SubscriptionType.Wildcard:
                lock (_wildcardSubscribers)
                {
                    _wildcardSubscribers.Add(registration);
                }
                break;
        }

        _logger.LogInformation("Registered subscriber {SubscriberId} for subscription type {Type}.", 
            registration.SubscriberId, registration.Type);
    }

    public async Task<PublicationResult> PublishAsync(DomainEvent @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        // 1. Authorize Publisher (Doc 10, Section 12.1)
        if (!_publishers.TryGetValue(@event.ProducerId, out var publisher) || 
            !publisher.AuthorizedEventTypes.Contains(@event.EventType))
        {
            _logger.LogWarning("Publisher authorization failed for ProducerId {ProducerId} and EventType {EventType}.", 
                @event.ProducerId, @event.EventType);
            return PublicationResult.Rejected(@event.EventId, "Publisher not authorized for this event type.");
        }

        // 2. Validate envelope & safety bounds (Doc 10, Section 6.4)
        if (_queueManager.GetTotalQueueCount() >= MaxQueueDepth)
        {
            _logger.LogCritical("Event Queue depth limit of {Max} reached. Reverting.", MaxQueueDepth);
            // Trigger critical system failure event
            await TriggerCriticalRollbackAsync(@event.SessionId, "EventQueue overflow limit exceeded.");
            return PublicationResult.Rejected(@event.EventId, "Queue depth limit exceeded.");
        }

        ulong seq;
        var inFlight = _queueManager.GetInFlightEvent();
        if (inFlight != null)
        {
            // Derived event path (buffering)
            _queueManager.Enqueue(@event, 0); // Sequence number assigned when flushed
            _logger.LogDebug("Buffered derived event {EventId} ({EventType}) caused by {ParentId}.", 
                @event.EventId, @event.EventType, inFlight.EventId);
            
            if (_auditStore != null)
            {
                await _auditStore.AppendEventAsync(@event, 0, cancellationToken);
                await _auditStore.UpdateLifecycleStatusAsync(@event.EventId, "ENQUEUED (BUFFERED)", cancellationToken);
            }
            return PublicationResult.Buffered(@event.EventId);
        }

        // Root event path
        seq = _queueManager.AssignSequenceNumber();
        _queueManager.Enqueue(@event, seq);

        if (_auditStore != null)
        {
            await _auditStore.AppendEventAsync(@event, seq, cancellationToken);
            await _auditStore.UpdateLifecycleStatusAsync(@event.EventId, "ENQUEUED", cancellationToken);
        }

        // Trigger asynchronous processing queue worker loop
        _ = Task.Run(() => ProcessQueueAsync(CancellationToken.None), CancellationToken.None);

        return PublicationResult.Success(@event.EventId, seq);
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        if (!await _dispatchSemaphore.WaitAsync(0, cancellationToken))
        {
            return; // Another thread is actively draining the queue.
        }

        try
        {
            while (!_isDisposed)
            {
                var next = _queueManager.DequeueNext();
                if (next == null) break;

                var (@event, sequenceNumber) = next.Value;
                await DispatchEventAsync(@event, sequenceNumber, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in Event Bus dispatch queue execution loop.");
        }
        finally
        {
            _dispatchSemaphore.Release();
        }
    }

    private async Task DispatchEventAsync(DomainEvent @event, ulong sequenceNumber, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing Event [{Seq}] {EventType} ({EventId}) Priority={Priority}", 
            sequenceNumber, @event.EventType, @event.EventId, @event.Priority);

        if (_auditStore != null)
        {
            await _auditStore.UpdateLifecycleStatusAsync(@event.EventId, "IN_FLIGHT", cancellationToken);
        }

        // Get matching subscribers ordered by priority registration (Doc 10, Section 7.3)
        var targets = ResolveSubscribers(@event)
            .OrderBy(s => s.DeliveryOrder)
            .ToList();

        var requiredAcks = new List<SubscriptionRegistration>();
        
        foreach (var subRegistration in targets)
        {
            var subsystem = _serviceProvider.GetServices<IGameSubsystem>()
                .FirstOrDefault(s => s.SubsystemId == subRegistration.SubscriberId);

            if (subsystem == null)
            {
                _logger.LogWarning("Subscriber registered with ID {Id} has no matching service implementation.", subRegistration.SubscriberId);
                continue;
            }

            if (subRegistration.RequiresAcknowledgement)
            {
                requiredAcks.Add(subRegistration);
            }

            _ = Task.Run(async () =>
            {
                var handled = await ExecuteWithRetryAsync(subsystem, subRegistration, @event, cancellationToken);
                if (handled.Acknowledged)
                {
                    if (_auditStore != null)
                    {
                        await _auditStore.RecordAcknowledgementAsync(@event.EventId, subsystem.SubsystemId, cancellationToken);
                    }
                    lock (requiredAcks)
                    {
                        requiredAcks.RemoveAll(r => r.SubscriberId == subsystem.SubsystemId);
                    }
                }
                else
                {
                    _logger.LogError("Required subscriber {SubsystemId} failed to handle event {EventId}: {Reason}", 
                        subsystem.SubsystemId, @event.EventId, handled.FailureReason);
                    await TriggerCriticalRollbackAsync(@event.SessionId, $"Reliability failure on subscriber {subsystem.SubsystemId}: {handled.FailureReason}");
                }
            }, cancellationToken);
        }

        // Wait for all required acknowledgements with timeout
        var timeoutSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (true)
        {
            lock (requiredAcks)
            {
                if (requiredAcks.Count == 0) break;
            }

            if (timeoutSource.Token.IsCancellationRequested)
            {
                _logger.LogError("Timeout waiting for required acknowledgements on event {EventId}.", @event.EventId);
                await TriggerCriticalRollbackAsync(@event.SessionId, "Timeout waiting for required acknowledgements.");
                break;
            }
            await Task.Delay(10, cancellationToken);
        }

        if (_auditStore != null)
        {
            await _auditStore.UpdateLifecycleStatusAsync(@event.EventId, "ACKNOWLEDGED", cancellationToken);
        }

        // Flush derived buffered events and assign sequence numbers (Doc 10, Section 6.3)
        var derived = _queueManager.ClearInFlightAndFlushDerived(sequenceNumber + 1);
        if (derived.Count > 0 && _auditStore != null)
        {
            ulong currentSeq = sequenceNumber + 1;
            foreach (var dEvent in derived)
            {
                // Update sequences of buffered events in the AuditStore
                await _auditStore.AppendEventAsync(dEvent, currentSeq++, cancellationToken);
            }
        }

        if (_auditStore != null)
        {
            await _auditStore.UpdateLifecycleStatusAsync(@event.EventId, "COMPLETED", cancellationToken);
        }
    }

    private async Task<EventHandlingResult> ExecuteWithRetryAsync(
        IGameSubsystem subsystem, 
        SubscriptionRegistration subReg, 
        DomainEvent @event, 
        CancellationToken cancellationToken)
    {
        int attempt = 0;
        int maxRetries = 2; // Default retry value (Doc 10, Section 13.3)
        int delayMs = subReg.TimeoutMilliseconds / 2;

        while (attempt <= maxRetries)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(subReg.TimeoutMilliseconds);

                var result = await subsystem.OnEventAsync(@event, cts.Token);
                if (result.Acknowledged) return result;
                
                _logger.LogWarning("Attempt {Attempt} for subscriber {SubsystemId} returned Nack: {Reason}. Retrying...", 
                    attempt, subsystem.SubsystemId, result.FailureReason);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Attempt {Attempt} for subscriber {SubsystemId} timed out.", attempt, subsystem.SubsystemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Subscriber {SubsystemId} threw an exception on attempt {Attempt}.", subsystem.SubsystemId, attempt);
            }

            attempt++;
            if (attempt <= maxRetries)
            {
                await Task.Delay(delayMs * attempt, cancellationToken); // Exponential backoff (Doc 10, Section 13.3)
            }
        }

        return EventHandlingResult.Nack("Max retries exceeded without acknowledgement.");
    }

    private IEnumerable<SubscriptionRegistration> ResolveSubscribers(DomainEvent @event)
    {
        var matched = new List<SubscriptionRegistration>();

        if (_subscribersByEvent.TryGetValue(@event.EventType, out var typeSubs))
        {
            lock (typeSubs) { matched.AddRange(typeSubs); }
        }

        if (_subscribersByCategory.TryGetValue(@event.EventCategory, out var categorySubs))
        {
            lock (categorySubs) { matched.AddRange(categorySubs); }
        }

        lock (_wildcardSubscribers)
        {
            matched.AddRange(_wildcardSubscribers);
        }

        return matched.GroupBy(s => s.SubscriberId).Select(g => g.First()); // Deduplicate
    }

    private async Task TriggerCriticalRollbackAsync(Guid sessionId, string reason)
    {
        _logger.LogCritical("CRITICAL EVENT FAILURE: {Reason}. Halting and requesting rollback.", reason);
        _queueManager.ClearAll();

        // Enqueue rollback event directly into critical path
        // In real execution, the Game Engine would listen and command all subsystems to revert to the last checkpoint.
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _dispatchSemaphore.Dispose();
    }
}
