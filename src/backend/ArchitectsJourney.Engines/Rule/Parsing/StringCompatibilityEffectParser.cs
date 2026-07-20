using System.Text.RegularExpressions;

namespace ArchitectsJourney.Engines.Rule.Parsing;

public sealed class StringCompatibilityEffectParser : IEffectParser
{
    public ParsedEffect? Parse(string sourceRuleId, string effectString)
    {
        if (string.IsNullOrWhiteSpace(effectString)) return null;

        var match = Regex.Match(effectString, @"^(?<type>[A-Za-z]+)(?:\(|:)(?<args>.*?)(?:\)?)$");
        if (!match.Success) return null;

        var typeStr = match.Groups["type"].Value;
        var argsStr = match.Groups["args"].Value;
        
        char[] splitChars = [',', ':'];
        var args = argsStr.Split(splitChars, StringSplitOptions.TrimEntries);

        return new ParsedEffect
        {
            SourceRuleId = sourceRuleId,
            Type = typeStr,
            Arg1 = args.Length > 0 ? args[0] : string.Empty,
            Arg2 = args.Length > 1 ? args[1] : string.Empty
        };
    }
}
