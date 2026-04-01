using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LootDistributionInfo;

public sealed record LootRollPlayerCandidate(string BaseName, string DisplayName, ushort? HomeWorldId, string? WorldName);

public sealed record LootRollItemCandidate(string ItemName, uint? ItemId);

public sealed record LootRollExtractionContext(
    IReadOnlyList<LootRollPlayerCandidate> PlayerCandidates,
    IReadOnlyList<LootRollItemCandidate> ItemCandidates,
    IReadOnlyList<int> IntParameters,
    ushort? LocalHomeWorldId,
    uint? LogMessageId);

public static class LootRollExtractor
{
    private static readonly Regex NeedGreedPattern = new(
        @"^(?<player>.+?)\s+(?:rolls?\s+)?(?<type>Need|Greed)\s+(?<value>\d{1,3})(?:\s+(?:on|for)\s+(?<item>.+?))?[.!]?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PassPattern = new(
        @"^(?<player>.+?)\s+passes?(?:\s+(?:on|for)\s+(?<item>.+?))?[.!]?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static LootRollObservation? TryExtract(
        string rawText,
        LootRollExtractionContext context,
        string zoneName,
        DateTimeOffset capturedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return null;
        }

        if (!TryDetermineRollType(rawText, out var rollType))
        {
            return null;
        }

        var parsedText = ParseFromText(rawText, rollType);
        var resolvedPlayer = ResolvePlayer(parsedText.PlayerName, rawText, context.PlayerCandidates, context.LocalHomeWorldId);
        if (string.IsNullOrWhiteSpace(resolvedPlayer.PlayerName))
        {
            return null;
        }

        var resolvedItem = ResolveItem(parsedText.ItemName, rawText, context.ItemCandidates);
        if (resolvedItem is null || string.IsNullOrWhiteSpace(resolvedItem.ItemKey))
        {
            return null;
        }

        var rollValue = parsedText.RollValue;
        if (!rollValue.HasValue && rollType is LootRollType.Need or LootRollType.Greed)
        {
            rollValue = context.IntParameters.LastOrDefault(value => value is >= 1 and <= 999);
            if (rollValue == 0)
            {
                rollValue = null;
            }
        }

        if (rollType is LootRollType.Need or LootRollType.Greed && !rollValue.HasValue)
        {
            return null;
        }

        var entry = new LootRollEntry
        {
            CapturedAtUtc = capturedAtUtc,
            PlayerName = resolvedPlayer.PlayerName!,
            PlayerDisplayName = resolvedPlayer.PlayerDisplayName ?? resolvedPlayer.PlayerName!,
            PlayerWorldName = resolvedPlayer.PlayerWorldName,
            PlayerHomeWorldId = resolvedPlayer.PlayerHomeWorldId,
            RollType = rollType,
            RollValue = rollValue,
            ItemKey = resolvedItem.ItemKey,
            ItemName = resolvedItem.ItemName,
        };
        entry.Normalize();

        return new LootRollObservation(entry, resolvedItem.ItemKey, resolvedItem.ItemName, zoneName, capturedAtUtc, rawText.Trim(), context.LogMessageId);
    }

    private static bool TryDetermineRollType(string rawText, out LootRollType rollType)
    {
        if (rawText.Contains("Need", StringComparison.OrdinalIgnoreCase))
        {
            rollType = LootRollType.Need;
            return true;
        }

        if (rawText.Contains("Greed", StringComparison.OrdinalIgnoreCase))
        {
            rollType = LootRollType.Greed;
            return true;
        }

        if (rawText.Contains("Pass", StringComparison.OrdinalIgnoreCase))
        {
            rollType = LootRollType.Pass;
            return true;
        }

        rollType = default;
        return false;
    }

    private static (string? PlayerName, string? ItemName, int? RollValue) ParseFromText(string rawText, LootRollType rollType)
    {
        var normalized = rawText.Trim();

        if (rollType is LootRollType.Need or LootRollType.Greed)
        {
            var match = NeedGreedPattern.Match(normalized);
            if (match.Success)
            {
                return (
                    NormalizeNullable(match.Groups["player"].Value),
                    NormalizeNullable(match.Groups["item"].Value),
                    int.TryParse(match.Groups["value"].Value, out var rollValue) ? rollValue : null);
            }
        }

        if (rollType == LootRollType.Pass)
        {
            var match = PassPattern.Match(normalized);
            if (match.Success)
            {
                return (
                    NormalizeNullable(match.Groups["player"].Value),
                    NormalizeNullable(match.Groups["item"].Value),
                    null);
            }
        }

        return (null, null, ExtractAnyRollNumber(normalized));
    }

    private static ResolvedRollPlayer ResolvePlayer(
        string? parsedPlayerName,
        string rawText,
        IReadOnlyList<LootRollPlayerCandidate> candidates,
        ushort? localHomeWorldId)
    {
        var cleanedCandidates = candidates
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.BaseName))
            .ToList();

        if (cleanedCandidates.Count == 1)
        {
            return ToResolvedPlayer(cleanedCandidates[0], localHomeWorldId);
        }

        if (!string.IsNullOrWhiteSpace(parsedPlayerName))
        {
            var exactMatch = cleanedCandidates
                .Where(candidate => string.Equals(candidate.BaseName, parsedPlayerName.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (exactMatch.Count == 1)
            {
                return ToResolvedPlayer(exactMatch[0], localHomeWorldId);
            }
        }

        var textMatch = cleanedCandidates
            .Where(candidate => rawText.Contains(candidate.BaseName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(candidate => candidate.BaseName.Length)
            .FirstOrDefault();
        if (textMatch is not null)
        {
            return ToResolvedPlayer(textMatch, localHomeWorldId);
        }

        var fallbackName = NormalizeNullable(parsedPlayerName);
        return fallbackName is null
            ? new ResolvedRollPlayer(null, null, null, null)
            : new ResolvedRollPlayer(
                fallbackName,
                FormatDisplayName(fallbackName, null, null, localHomeWorldId),
                null,
                null);
    }

    private static ResolvedRollItem? ResolveItem(string? parsedItemName, string rawText, IReadOnlyList<LootRollItemCandidate> candidates)
    {
        var cleanedCandidates = candidates
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.ItemName))
            .ToList();

        if (cleanedCandidates.Count == 1)
        {
            return ToResolvedItem(cleanedCandidates[0]);
        }

        if (!string.IsNullOrWhiteSpace(parsedItemName))
        {
            var normalizedParsedName = LootItemKey.NormalizeItemName(parsedItemName);
            var exactMatch = cleanedCandidates
                .Where(candidate => LootItemKey.NormalizeItemName(candidate.ItemName) == normalizedParsedName)
                .ToList();
            if (exactMatch.Count == 1)
            {
                return ToResolvedItem(exactMatch[0]);
            }
        }

        var textMatch = cleanedCandidates
            .Where(candidate => rawText.Contains(candidate.ItemName, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(candidate => candidate.ItemName.Length)
            .FirstOrDefault();
        if (textMatch is not null)
        {
            return ToResolvedItem(textMatch);
        }

        var fallbackName = NormalizeNullable(parsedItemName);
        if (fallbackName is null)
        {
            return null;
        }

        var fallbackKey = LootItemKey.Build(null, fallbackName);
        return fallbackKey is null ? null : new ResolvedRollItem(fallbackKey, fallbackName);
    }

    private static int? ExtractAnyRollNumber(string rawText)
    {
        var matches = Regex.Matches(rawText, @"\b(?<value>\d{1,3})\b");
        if (matches.Count == 0)
        {
            return null;
        }

        var valueText = matches[^1].Groups["value"].Value;
        return int.TryParse(valueText, out var parsedValue) ? parsedValue : null;
    }

    private static ResolvedRollPlayer ToResolvedPlayer(LootRollPlayerCandidate candidate, ushort? localHomeWorldId)
    {
        var baseName = candidate.BaseName.Trim();
        return new ResolvedRollPlayer(
            baseName,
            FormatDisplayName(baseName, candidate.WorldName, candidate.HomeWorldId, localHomeWorldId),
            NormalizeNullable(candidate.WorldName),
            candidate.HomeWorldId);
    }

    private static ResolvedRollItem? ToResolvedItem(LootRollItemCandidate candidate)
    {
        var itemName = candidate.ItemName.Trim();
        var itemKey = LootItemKey.Build(candidate.ItemId, itemName);
        return itemKey is null ? null : new ResolvedRollItem(itemKey, itemName);
    }

    private static string FormatDisplayName(string baseName, string? worldName, ushort? worldId, ushort? localHomeWorldId)
    {
        if (string.IsNullOrWhiteSpace(worldName)
            || !worldId.HasValue
            || !localHomeWorldId.HasValue
            || worldId.Value == 0
            || worldId.Value == localHomeWorldId.Value)
        {
            return baseName;
        }

        return $"{baseName} ({worldName.Trim()})";
    }

    private static string? NormalizeNullable(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private sealed record ResolvedRollPlayer(string? PlayerName, string? PlayerDisplayName, string? PlayerWorldName, ushort? PlayerHomeWorldId);

    private sealed record ResolvedRollItem(string ItemKey, string ItemName);
}
