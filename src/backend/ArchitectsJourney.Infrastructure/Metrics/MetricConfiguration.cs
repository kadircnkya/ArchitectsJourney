using ArchitectsJourney.Application.Contracts;
using Microsoft.Extensions.Configuration;

namespace ArchitectsJourney.Infrastructure.Metrics;

public sealed class MetricConfiguration : IMetricConfiguration
{
    private readonly int[] _thresholds;

    public MetricConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection("MetricEngine:Thresholds");
        var thresholdsConfig = section.Exists() 
            ? section.GetChildren().Select(x => int.Parse(x.Value!)).ToArray() 
            : null;
        
        _thresholds = thresholdsConfig ?? [25, 50, 75];
    }

    public IReadOnlyList<int> Thresholds => _thresholds;
}
