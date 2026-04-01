using System;

namespace LootDistributionInfo;

public static class LootItemKey
{
    public static string? Build(LootRecord record)
    {
        return Build(record.ItemId, record.ResolvedItemName ?? record.ItemName);
    }

    public static string? Build(uint? itemId, string? itemName)
    {
        if (itemId is uint resolvedItemId)
        {
            return $"item:{resolvedItemId}";
        }

        var normalizedName = NormalizeItemName(itemName);
        return normalizedName.Length == 0 ? null : $"name:{normalizedName}";
    }

    public static string NormalizeItemName(string? itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            return string.Empty;
        }

        var trimmed = itemName.Trim();
        var quantityEnd = 0;
        while (quantityEnd < trimmed.Length && char.IsDigit(trimmed[quantityEnd]))
        {
            quantityEnd++;
        }

        if (quantityEnd > 0 && quantityEnd < trimmed.Length && trimmed[quantityEnd] == ' ')
        {
            trimmed = trimmed[(quantityEnd + 1)..];
        }

        if (trimmed.EndsWith(" HQ", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[..^3];
        }

        foreach (var prefix in new[] { "a ", "an ", "the " })
        {
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed[prefix.Length..];
                break;
            }
        }

        return LootMatcher.NormalizeForMatch(trimmed).Trim();
    }

    public static string GetDisplayLabel(string? itemKey, string? fallbackItemName)
    {
        if (!string.IsNullOrWhiteSpace(fallbackItemName))
        {
            return fallbackItemName.Trim();
        }

        if (string.IsNullOrWhiteSpace(itemKey))
        {
            return "Unknown";
        }

        if (itemKey.StartsWith("item:", StringComparison.Ordinal))
        {
            return $"Item #{itemKey["item:".Length..]}";
        }

        if (itemKey.StartsWith("name:", StringComparison.Ordinal))
        {
            return itemKey["name:".Length..];
        }

        return itemKey;
    }
}
