using System;
using System.Text.RegularExpressions;

namespace LootDistributionInfo;

public static partial class LootRollMatcher
{
    public static LootRollRecord? TryMatch(string rawText)
    {
        var trimmedText = rawText.Trim();
        if (trimmedText.Length == 0)
        {
            return null;
        }

        foreach (var pattern in Patterns)
        {
            var match = pattern.Match(trimmedText);
            if (!match.Success)
            {
                continue;
            }

            var playerName = NormalizeText(match.Groups["player"].Value);
            var itemName = NormalizeText(match.Groups["item"].Value);
            if (string.IsNullOrWhiteSpace(playerName) || string.IsNullOrWhiteSpace(itemName))
            {
                return null;
            }

            var rollType = ParseRollType(match.Groups["type"].Value);
            if (rollType is null)
            {
                return null;
            }

            var record = new LootRollRecord
            {
                RawText = trimmedText,
                PlayerName = playerName,
                ItemName = StripLeadingArticle(itemName),
                RollType = rollType.Value,
                RollValue = ParseRollValue(match.Groups["value"].Value, rollType.Value),
            };

            if (record.RollType != LootRollType.Pass && record.RollValue is null)
            {
                return null;
            }

            record.Normalize();
            return record;
        }

        return null;
    }

    public static string NormalizeItemKey(string itemName)
    {
        return LootMatcher.NormalizeForMatch(StripLeadingArticle(itemName)).Trim();
    }

    private static LootRollType? ParseRollType(string rawValue)
    {
        return rawValue.Trim().ToLowerInvariant() switch
        {
            "need" => LootRollType.Need,
            "greed" => LootRollType.Greed,
            "pass" => LootRollType.Pass,
            "passes" => LootRollType.Pass,
            "passed" => LootRollType.Pass,
            _ => null,
        };
    }

    private static int? ParseRollValue(string rawValue, LootRollType rollType)
    {
        if (rollType == LootRollType.Pass)
        {
            return null;
        }

        return int.TryParse(rawValue, out var value) ? value : null;
    }

    private static string NormalizeText(string value)
    {
        return value.Trim().TrimEnd('.', '!', '?');
    }

    private static string StripLeadingArticle(string itemText)
    {
        foreach (var prefix in new[] { "a ", "an ", "the " })
        {
            if (itemText.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return itemText[prefix.Length..];
            }
        }

        return itemText;
    }

    private static Regex[] Patterns =>
    [
        RollOnPattern(),
        RollForPattern(),
        RollColonPattern(),
        PassOnPattern(),
    ];

    [GeneratedRegex(@"^(?<player>.+?)\s+rolls\s+(?<type>Need|Greed|Pass)\s*(?<value>\d{1,3})?\s*on\s+(?<item>.+?)[.!]?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RollOnPattern();

    [GeneratedRegex(@"^(?<player>.+?)\s+rolls\s+(?<type>Need|Greed)\s*(?<value>\d{1,3})?\s*for\s+(?<item>.+?)[.!]?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RollForPattern();

    [GeneratedRegex(@"^(?<player>.+?)\s+rolls\s+(?<type>Need|Greed)\s+for\s+(?<item>.+?)\s*:\s*(?<value>\d{1,3})[.!]?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RollColonPattern();

    [GeneratedRegex(@"^(?<player>.+?)\s+(?<type>passes|passed|pass)\s+on\s+(?<item>.+?)[.!]?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PassOnPattern();
}
