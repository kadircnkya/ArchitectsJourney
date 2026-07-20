namespace ArchitectsJourney.Engines.Rule.Parsing;

public sealed class ParsedEffect
{
    public required string SourceRuleId { get; init; }
    public required string Type { get; init; }
    public required string Arg1 { get; init; }
    public required string Arg2 { get; init; }
}

public interface IEffectParser
{
    ParsedEffect? Parse(string sourceRuleId, string effectString);
}
