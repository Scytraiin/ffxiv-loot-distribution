using System;
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

    public static LootRecord? TryMatch(string rawText, LootCaptureSource source, DateTimeOffset capturedAtUtc)
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

        var (playerName, itemText) = SplitBestEffort(trimmedText);

        return new LootRecord
        {
            CapturedAtUtc = capturedAtUtc,
            RawText = trimmedText,
            PlayerName = playerName,
            ItemText = itemText,
            Source = source,
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

    private static (string? PlayerName, string? ItemText) SplitBestEffort(string rawText)
    {
        // Best-effort splitting is enough for the first version; the raw line remains the source
        // of truth in the UI even when player/item extraction is incomplete.
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
}
