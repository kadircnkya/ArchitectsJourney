using ArchitectsJourney.Domain.Common;

namespace ArchitectsJourney.Domain.ValueObjects;

public sealed class MetricHistoryEntry : ValueObject
{
    public required Guid CausationId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required IReadOnlyDictionary<string, int> Metrics { get; init; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return CausationId;
        yield return Timestamp;
    }
}
