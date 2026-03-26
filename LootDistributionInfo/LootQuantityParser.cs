using System;

namespace LootDistributionInfo;

public static class LootQuantityParser
{
    public static (int Quantity, string? ItemName) Split(string? lootText)
    {
        if (string.IsNullOrWhiteSpace(lootText))
        {
            return (1, null);
        }

        var trimmed = lootText.Trim();
        var quantityEnd = 0;
        while (quantityEnd < trimmed.Length && char.IsDigit(trimmed[quantityEnd]))
        {
            quantityEnd++;
        }

        if (quantityEnd > 0
            && quantityEnd < trimmed.Length
            && trimmed[quantityEnd] == ' '
            && int.TryParse(trimmed[..quantityEnd], out var quantity)
            && quantity > 0)
        {
            var itemName = NormalizeNullable(trimmed[(quantityEnd + 1)..]);
            return (quantity, itemName);
        }

        return (1, NormalizeNullable(trimmed));
    }

    private static string? NormalizeNullable(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
