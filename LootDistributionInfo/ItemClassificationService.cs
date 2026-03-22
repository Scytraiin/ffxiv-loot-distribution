using System;
using System.Collections.Generic;
using System.Reflection;

using Dalamud.Plugin.Services;

using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;

namespace LootDistributionInfo;

public sealed class ItemClassificationService
{
    private static readonly (string PropertyName, string Label)[] EquipSlotLabels =
    [
        ("MainHand", "Main Hand"),
        ("OffHand", "Off Hand"),
        ("Shield", "Shield"),
        ("SoulCrystal", "Soul Crystal"),
        ("Head", "Head Equipment"),
        ("Body", "Body Equipment"),
        ("Hands", "Hands Equipment"),
        ("Gloves", "Hands Equipment"),
        ("Arms", "Hands Equipment"),
        ("Legs", "Legs Equipment"),
        ("Feet", "Feet Equipment"),
        ("Ears", "Earrings"),
        ("Neck", "Necklace"),
        ("Wrists", "Bracelets"),
        ("FingerL", "Ring"),
        ("FingerR", "Ring"),
    ];

    private readonly Dictionary<string, ItemClassificationResult> classificationByLookupKey = new(StringComparer.Ordinal);
    private readonly Dictionary<uint, ItemClassificationResult> classificationByItemId = [];

    public ItemClassificationService(IDataManager dataManager)
    {
        foreach (var item in dataManager.GetExcelSheet<Item>())
        {
            var resolvedItemName = ExtractText(item.Name);
            var lookupKey = NormalizeLookupKey(resolvedItemName);
            if (lookupKey.Length == 0 || this.classificationByLookupKey.ContainsKey(lookupKey))
            {
                continue;
            }

            var result = CreateResult(item, resolvedItemName);
            this.classificationByLookupKey[lookupKey] = result;

            if (result.ItemId is uint itemId)
            {
                this.classificationByItemId[itemId] = result;
            }
        }
    }

    public ItemClassificationResult Classify(string? lootText, uint? itemId = null)
    {
        if (itemId is uint knownItemId && this.classificationByItemId.TryGetValue(knownItemId, out var itemResult))
        {
            return itemResult;
        }

        var lookupKey = NormalizeLookupKey(lootText);
        return lookupKey.Length != 0 && this.classificationByLookupKey.TryGetValue(lookupKey, out var result)
            ? result
            : ItemClassificationResult.Unresolved();
    }

    internal static string NormalizeLookupKey(string? itemName)
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

    private static ItemClassificationResult CreateResult(Item item, string resolvedItemName)
    {
        var itemId = item.RowId == 0 ? null : (uint?)item.RowId;
        byte? filterGroupId = item.FilterGroup == 0 ? null : (byte?)item.FilterGroup;
        var filterGroupLabel = filterGroupId is byte filterGroup ? ItemCategoryMappings.GetFilterGroupLabel(filterGroup) : null;
        uint? equipSlotCategoryId = item.EquipSlotCategory.RowId == 0 ? null : (uint?)item.EquipSlotCategory.RowId;
        var equipSlotCategoryLabel = ResolveEquipSlotCategoryLabel(item.EquipSlotCategory.ValueNullable);

        return new ItemClassificationResult
        {
            ItemId = itemId,
            IconId = item.Icon == 0 ? null : (uint?)item.Icon,
            Rarity = item.Rarity == 0 ? null : (uint?)item.Rarity,
            FilterGroupId = filterGroupId,
            FilterGroupLabel = filterGroupLabel,
            EquipSlotCategoryId = equipSlotCategoryId,
            EquipSlotCategoryLabel = equipSlotCategoryLabel,
            ItemUICategoryId = item.ItemUICategory.RowId == 0 ? null : (uint?)item.ItemUICategory.RowId,
            ItemSearchCategoryId = item.ItemSearchCategory.RowId == 0 ? null : (uint?)item.ItemSearchCategory.RowId,
            ItemSortCategoryId = item.ItemSortCategory.RowId == 0 ? null : (uint?)item.ItemSortCategory.RowId,
            ResolvedItemName = resolvedItemName,
            ClassificationSource = "ItemSheet",
        };
    }

    private static string ExtractText(ReadOnlySeString value)
    {
        return value.ExtractText().Trim();
    }

    private static string? ResolveEquipSlotCategoryLabel(object? equipSlotCategory)
    {
        if (equipSlotCategory is null)
        {
            return null;
        }

        var type = equipSlotCategory.GetType();
        foreach (var (propertyName, label) in EquipSlotLabels)
        {
            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property?.PropertyType != typeof(sbyte))
            {
                continue;
            }

            if ((sbyte)(property.GetValue(equipSlotCategory) ?? (sbyte)0) != 0)
            {
                return label;
            }
        }

        return null;
    }
}
