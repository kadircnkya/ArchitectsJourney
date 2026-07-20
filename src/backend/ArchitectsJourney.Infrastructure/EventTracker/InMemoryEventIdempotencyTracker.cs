using System.Collections.Concurrent;
using ArchitectsJourney.Application.Contracts;

namespace ArchitectsJourney.Infrastructure.EventTracker;

public sealed class InMemoryEventIdempotencyTracker : IEventIdempotencyTracker
{
    // Maps PlaythroughId -> (EventId -> DummyByte)
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, byte>> _processedEvents = new();

    public Task<bool> TryMarkProcessedAsync(Guid playthroughId, Guid eventId, CancellationToken cancellationToken = default)
    {
        var eventSet = _processedEvents.GetOrAdd(playthroughId, _ => new ConcurrentDictionary<Guid, byte>());
        
        // TryAdd returns true if the key was successfully added, meaning it wasn't processed yet.
        var added = eventSet.TryAdd(eventId, 1);
        
        return Task.FromResult(added);
    }
}
