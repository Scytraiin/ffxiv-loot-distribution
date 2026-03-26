using System;
using System.Collections.Generic;
using System.Linq;

namespace LootDistributionInfo;

[Serializable]
public enum LootHistoryQuickFilter
{
    All = 0,
    Self = 1,
    Dungeon = 2,
    Raid = 3,
    Favorites = 4,
}

[Serializable]
public enum LootHistoryGroupingMode
{
    Flat = 0,
    ByZone = 1,
    ByItem = 2,
    ByRecipient = 3,
}

[Serializable]
public enum LootHistorySortMode
{
    NewestFirst = 0,
    OldestFirst = 1,
    QuantityHighToLow = 2,
    ItemName = 3,
    RecipientName = 4,
}

public enum LootRecipientFilter
{
    All = 0,
    Self = 1,
    PartyAlliance = 2,
    Other = 3,
}

public sealed record LootHistoryBrowseOptions(
    string SearchText,
    LootHistoryQuickFilter QuickFilter,
    LootHistorySortMode SortMode,
    LootRecipientFilter RecipientFilter,
    string SelectedCategory,
    string SelectedZone,
    IReadOnlyCollection<uint> FavoriteItemIds);

public sealed record LootHistoryGroup(string Label, IReadOnlyList<LootRecord> Records);

public static class LootHistoryBrowser
{
    public static IReadOnlyList<LootRecord> FilterAndSort(IEnumerable<LootRecord> records, LootHistoryBrowseOptions options)
    {
        var filtered = records
            .Where(record => MatchesSearch(record, options.SearchText))
            .Where(record => MatchesQuickFilter(record, options.QuickFilter, options.FavoriteItemIds))
            .Where(record => MatchesRecipientFilter(record, options.RecipientFilter))
            .Where(record => MatchesCategory(record, options.SelectedCategory))
            .Where(record => MatchesZone(record, options.SelectedZone));

        return ApplySort(filtered, options.SortMode).ToList();
    }

    public static IReadOnlyList<LootHistoryGroup> Group(IReadOnlyList<LootRecord> records, LootHistoryGroupingMode groupingMode)
    {
        if (records.Count == 0)
        {
            return [];
        }

        if (groupingMode == LootHistoryGroupingMode.Flat)
        {
            return [new LootHistoryGroup("History", records)];
        }

        // Preserve the already-sorted record order while forming visual groups so the UI can
        // switch between flat and grouped browsing without reordering records unexpectedly.
        var grouped = new List<LootHistoryGroup>();
        foreach (var record in records)
        {
            var label = GetGroupingLabel(record, groupingMode);
            var existingGroup = grouped.FirstOrDefault(group => string.Equals(group.Label, label, StringComparison.OrdinalIgnoreCase));
            if (existingGroup is null)
            {
                grouped.Add(new LootHistoryGroup(label, [record]));
                continue;
            }

            var mutableRecords = existingGroup.Records.ToList();
            mutableRecords.Add(record);
            grouped[grouped.IndexOf(existingGroup)] = existingGroup with { Records = mutableRecords };
        }

        return grouped;
    }

    public static string GetDisplayItemName(LootRecord record)
    {
        return record.ResolvedItemName ?? record.ItemName ?? record.RawText;
    }

    public static string GetRecipientLabel(LootRecord record)
    {
        return record.WhoDisplayName ?? record.WhoName ?? "Unknown";
    }

    public static string GetQuickFilterLabel(LootHistoryQuickFilter quickFilter)
    {
        return quickFilter switch
        {
            LootHistoryQuickFilter.Self => "Self",
            LootHistoryQuickFilter.Dungeon => "Dungeon",
            LootHistoryQuickFilter.Raid => "Raid",
            LootHistoryQuickFilter.Favorites => "Favorites",
            _ => "All",
        };
    }

    public static string GetGroupingLabel(LootHistoryGroupingMode groupingMode)
    {
        return groupingMode switch
        {
            LootHistoryGroupingMode.ByZone => "By Zone",
            LootHistoryGroupingMode.ByItem => "By Item",
            LootHistoryGroupingMode.ByRecipient => "By Recipient",
            _ => "Flat",
        };
    }

    public static string GetSortLabel(LootHistorySortMode sortMode)
    {
        return sortMode switch
        {
            LootHistorySortMode.OldestFirst => "Oldest first",
            LootHistorySortMode.QuantityHighToLow => "Quantity high to low",
            LootHistorySortMode.ItemName => "Item name",
            LootHistorySortMode.RecipientName => "Recipient name",
            _ => "Newest first",
        };
    }

    private static string GetGroupingLabel(LootRecord record, LootHistoryGroupingMode groupingMode)
    {
        return groupingMode switch
        {
            LootHistoryGroupingMode.ByZone => string.IsNullOrWhiteSpace(record.ZoneName) ? "Unknown Zone" : record.ZoneName,
            LootHistoryGroupingMode.ByItem => GetDisplayItemName(record),
            LootHistoryGroupingMode.ByRecipient => GetRecipientLabel(record),
            _ => "History",
        };
    }

    private static bool MatchesSearch(LootRecord record, string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        return Contains(record.ZoneName, searchText)
            || Contains(record.WhoName, searchText)
            || Contains(record.WhoDisplayName, searchText)
            || Contains(record.WhoWorldName, searchText)
            || Contains(record.ItemName, searchText)
            || Contains(GetDisplayItemName(record), searchText)
            || Contains(record.ItemCategoryLabel, searchText)
            || Contains(record.FilterGroupLabel, searchText)
            || Contains(record.EquipSlotCategoryLabel, searchText)
            || Contains(record.RawText, searchText)
            || Contains(record.Quantity.ToString(), searchText);
    }

    private static bool MatchesQuickFilter(LootRecord record, LootHistoryQuickFilter quickFilter, IReadOnlyCollection<uint> favoriteItemIds)
    {
        return quickFilter switch
        {
            LootHistoryQuickFilter.Self => record.WhoConfidence == LootWhoConfidence.Self,
            LootHistoryQuickFilter.Dungeon => record.LootTypeBucket == LootTypeBucket.Dungeon,
            LootHistoryQuickFilter.Raid => record.LootTypeBucket == LootTypeBucket.Raid,
            LootHistoryQuickFilter.Favorites => record.ItemId is uint itemId && favoriteItemIds.Contains(itemId),
            _ => true,
        };
    }

    private static bool MatchesRecipientFilter(LootRecord record, LootRecipientFilter recipientFilter)
    {
        return recipientFilter switch
        {
            LootRecipientFilter.Self => record.WhoConfidence == LootWhoConfidence.Self,
            LootRecipientFilter.PartyAlliance => record.WhoConfidence == LootWhoConfidence.PartyOrAllianceVerified,
            LootRecipientFilter.Other => record.WhoConfidence != LootWhoConfidence.Self && record.WhoConfidence != LootWhoConfidence.PartyOrAllianceVerified,
            _ => true,
        };
    }

    private static bool MatchesCategory(LootRecord record, string selectedCategory)
    {
        if (string.IsNullOrWhiteSpace(selectedCategory))
        {
            return true;
        }

        return string.Equals(record.ItemCategoryLabel ?? "Unknown", selectedCategory, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesZone(LootRecord record, string selectedZone)
    {
        if (string.IsNullOrWhiteSpace(selectedZone))
        {
            return true;
        }

        return string.Equals(string.IsNullOrWhiteSpace(record.ZoneName) ? "Unknown" : record.ZoneName, selectedZone, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<LootRecord> ApplySort(IEnumerable<LootRecord> records, LootHistorySortMode sortMode)
    {
        return sortMode switch
        {
            LootHistorySortMode.OldestFirst => records
                .OrderBy(record => record.CapturedAtUtc)
                .ThenBy(record => GetDisplayItemName(record), StringComparer.OrdinalIgnoreCase),

            LootHistorySortMode.QuantityHighToLow => records
                .OrderByDescending(record => record.Quantity)
                .ThenByDescending(record => record.CapturedAtUtc)
                .ThenBy(record => GetDisplayItemName(record), StringComparer.OrdinalIgnoreCase),

            LootHistorySortMode.ItemName => records
                .OrderBy(record => GetDisplayItemName(record), StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(record => record.CapturedAtUtc),

            LootHistorySortMode.RecipientName => records
                .OrderBy(record => GetRecipientLabel(record), StringComparer.OrdinalIgnoreCase)
                .ThenByDescending(record => record.CapturedAtUtc),

            _ => records
                .OrderByDescending(record => record.CapturedAtUtc)
                .ThenBy(record => GetDisplayItemName(record), StringComparer.OrdinalIgnoreCase),
        };
    }

    private static bool Contains(string? value, string searchText)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }
}
