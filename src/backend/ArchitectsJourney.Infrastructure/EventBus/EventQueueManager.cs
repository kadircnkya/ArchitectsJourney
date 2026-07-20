using ArchitectsJourney.Application.Common;
using ArchitectsJourney.Application.Events;

namespace ArchitectsJourney.Infrastructure.EventBus;

/// <summary>
/// In-memory queue manager supporting priorities, FIFO within priority tier, 
/// and buffering derived events until in-flight completion.
/// Implements Document 10, Sections 6 and 8.
/// </summary>
internal sealed class EventQueueManager
{
    private readonly PriorityQueue<DomainEvent, EventQueuePriority> _queue = new();
    private readonly List<DomainEvent> _derivedBuffer = new();
    private DomainEvent? _inFlight;
    private ulong _globalSequenceCounter = 0;

    private readonly object _lock = new();

    public record EventQueuePriority(EventPriority Priority, ulong SequenceNumber) : IComparable<EventQueuePriority>
    {
        public int CompareTo(EventQueuePriority? other)
        {
            if (other is null) return -1;
            
            // Compare priorities first (lower priority enum numerical value is higher priority)
            int priorityComparison = Priority.CompareTo(other.Priority);
            if (priorityComparison != 0) return priorityComparison;

            // FIFO order within same priority tier
            return SequenceNumber.CompareTo(other.SequenceNumber);
        }
    }

    public ulong AssignSequenceNumber()
    {
        lock (_lock)
        {
            return ++_globalSequenceCounter;
        }
    }

    public void Enqueue(DomainEvent @event, ulong sequenceNumber)
    {
        lock (_lock)
        {
            if (_inFlight != null)
            {
                // Derived event buffering during active dispatch (Doc 10, Section 6.3)
                _derivedBuffer.Add(@event);
            }
            else
            {
                var priorityKey = new EventQueuePriority(@event.Priority, sequenceNumber);
                _queue.Enqueue(@event, priorityKey);
            }
        }
    }

    public (DomainEvent Event, ulong SequenceNumber)? DequeueNext()
    {
        lock (_lock)
        {
            if (_queue.Count == 0) return null;
            
            if (_queue.TryDequeue(out var @event, out var priorityKey))
            {
                _inFlight = @event;
                return (@event, priorityKey.SequenceNumber);
            }

            return null;
        }
    }

    public DomainEvent? GetInFlightEvent()
    {
        lock (_lock)
        {
            return _inFlight;
        }
    }

    public IReadOnlyList<DomainEvent> ClearInFlightAndFlushDerived(ulong baseSequenceStart)
    {
        lock (_lock)
        {
            _inFlight = null;
            
            var flushed = new List<DomainEvent>(_derivedBuffer);
            _derivedBuffer.Clear();

            // Enqueue all buffered derived events into the primary queue now (Doc 10, Section 6.3)
            ulong sequence = baseSequenceStart;
            foreach (var @event in flushed)
            {
                var priorityKey = new EventQueuePriority(@event.Priority, sequence++);
                _queue.Enqueue(@event, priorityKey);
            }

            return flushed;
        }
    }

    public void ClearAll()
    {
        lock (_lock)
        {
            _queue.Clear();
            _derivedBuffer.Clear();
            _inFlight = null;
            _globalSequenceCounter = 0;
        }
    }

    public int GetTotalQueueCount()
    {
        lock (_lock)
        {
            return _queue.Count + _derivedBuffer.Count;
        }
    }
}
