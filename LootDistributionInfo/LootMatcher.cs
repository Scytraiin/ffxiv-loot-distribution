using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LootDistributionInfo;

public static class LootMatcher
{
    private static readonly string[] VerbNeedles =
    [
        " obtain ",
        " obtained ",
        " obtains ",
    ];

    public static LootParseResult? TryMatch(string rawText)
    {
        var trimmedText = rawText.Trim();
        if (trimmedText.Length == 0)
        {
            return null;
        }

        if (!ContainsLootVerb(trimmedText))
        {
            return null;
        }

        var (subjectText, lootText) = SplitBestEffort(trimmedText);
        if (string.IsNullOrWhiteSpace(lootText))
        {
            return null;
        }

        var (quantity, itemName) = LootQuantityParser.Split(lootText);
        if (string.IsNullOrWhiteSpace(itemName))
        {
            return null;
        }

        return new LootParseResult
        {
            RawText = trimmedText,
            SubjectText = subjectText,
            Quantity = quantity,
            ItemName = itemName,
        };
    }

    public static bool ContainsLootVerb(string rawText)
    {
        // The matcher stays intentionally simple in v1: normalize punctuation to spaces and look
        // for whole-word loot verbs with plain string matching instead of building a parser table.
        var normalized = NormalizeForMatch(rawText);
        foreach (var needle in VerbNeedles)
        {
            if (normalized.Contains(needle, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static string NormalizeForMatch(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(rawText.Length + 2);
        builder.Append(' ');

        var previousWasSeparator = true;
        foreach (var character in rawText)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                previousWasSeparator = false;
                continue;
            }

            if (!previousWasSeparator)
            {
                builder.Append(' ');
                previousWasSeparator = true;
            }
        }

        if (!previousWasSeparator)
        {
            builder.Append(' ');
        }

        return builder.ToString();
    }

    private static (string? SubjectText, string? LootText) SplitBestEffort(string rawText)
    {
        var paddedText = $" {rawText} ";
        var loweredText = paddedText.ToLowerInvariant();

        foreach (var marker in VerbNeedles)
        {
            var index = loweredText.IndexOf(marker, StringComparison.Ordinal);
            if (index < 0)
            {
                continue;
            }

            var playerName = paddedText[..index].Trim();
            var itemText = paddedText[(index + marker.Length)..].Trim();

            itemText = itemText.TrimEnd('.', '!', '?');
            itemText = StripLeadingArticle(itemText);

            return
            (
                playerName.Length == 0 ? null : playerName,
                itemText.Length == 0 ? null : itemText
            );
        }

        return (null, null);
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

    public static bool LooksLikeTwoWordName(string candidate)
    {
        var parts = candidate.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 2 && parts.All(IsLikelyNamePart);
    }

    private static bool IsLikelyNamePart(string part)
    {
        if (part.Length < 2)
        {
            return false;
        }

        if (!char.IsUpper(part[0]))
        {
            return false;
        }

        foreach (var character in part)
        {
            if (char.IsLetter(character) || character is '\'' or '-')
            {
                continue;
            }

            return false;
        }

        return true;
    }

    public static string NormalizeForNameMatch(string value)
    {
        return string.Join(
            ' ',
            value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => part.ToLowerInvariant()));
    }
}
