using System;
using System.Collections.Generic;

using Dalamud.Configuration;
using Dalamud.Plugin;

namespace LootDistributionInfo;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public const int DefaultMaxEntries = 500;
    public const int CurrentVersion = 11;

    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public int Version { get; set; } = CurrentVersion;

    public bool RetainHistoryBetweenSessions { get; set; } = true;

    public int MaxEntries { get; set; } = DefaultMaxEntries;

    public bool DebugModeEnabled { get; set; }

    public bool ShowItemIcons { get; set; } = true;

    public bool ShowItemTooltips { get; set; } = true;

    public bool ShowOnlySelfLoot { get; set; }

    public bool UseCompactMainWindowByDefault { get; set; }

    public LootHistoryQuickFilter DefaultQuickFilter { get; set; } = LootHistoryQuickFilter.All;

    public LootHistoryGroupingMode DefaultGroupingMode { get; set; } = LootHistoryGroupingMode.Flat;

    public LootHistorySortMode DefaultSortMode { get; set; } = LootHistorySortMode.NewestFirst;

    public LootHistoryColumnVisibility LootHistoryColumns { get; set; } = new();

    public ItemDetailsColumnVisibility ItemDetailsColumns { get; set; } = new();

    public List<string> HiddenCategoryLabels { get; set; } = [];

    public List<uint> BlacklistedItemIds { get; set; } = [];

    public List<uint> FavoriteItemIds { get; set; } = [];

    public List<string> BlacklistedItemKeys { get; set; } = [];

    public List<string> FavoriteItemKeys { get; set; } = [];

    public List<LootRecord> StoredRecords { get; set; } = [];

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
        this.MigrateFromLegacyRecords();
        this.Normalize();
    }

    public void Save()
    {
        this.pluginInterface?.SavePluginConfig(this);
    }

    public void Normalize()
    {
        this.Version = CurrentVersion;
        this.MaxEntries = Math.Clamp(this.MaxEntries, 1, 5000);
        this.StoredRecords ??= [];
        this.HiddenCategoryLabels ??= [];
        this.BlacklistedItemIds ??= [];
        this.FavoriteItemIds ??= [];
        this.BlacklistedItemKeys ??= [];
        this.FavoriteItemKeys ??= [];
        this.LootHistoryColumns ??= new LootHistoryColumnVisibility();
        this.ItemDetailsColumns ??= new ItemDetailsColumnVisibility();
        this.HiddenCategoryLabels = this.HiddenCategoryLabels
            .Select(NormalizeNullable)
            .Where(value => value is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
        this.BlacklistedItemIds = this.BlacklistedItemIds
            .Distinct()
            .OrderBy(value => value)
            .ToList();
        this.FavoriteItemIds = this.FavoriteItemIds
            .Distinct()
            .OrderBy(value => value)
            .ToList();
        this.BlacklistedItemKeys = this.BlacklistedItemKeys
            .Select(NormalizeNullable)
            .Where(value => value is not null)
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToList();
        this.FavoriteItemKeys = this.FavoriteItemKeys
            .Select(NormalizeNullable)
            .Where(value => value is not null)
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToList();
        this.DefaultQuickFilter = Enum.IsDefined(this.DefaultQuickFilter) ? this.DefaultQuickFilter : LootHistoryQuickFilter.All;
        this.DefaultGroupingMode = Enum.IsDefined(this.DefaultGroupingMode) ? this.DefaultGroupingMode : LootHistoryGroupingMode.Flat;
        this.DefaultSortMode = Enum.IsDefined(this.DefaultSortMode) ? this.DefaultSortMode : LootHistorySortMode.NewestFirst;

        foreach (var record in this.StoredRecords)
        {
            record.Normalize();
        }

        if (this.StoredRecords.Count > this.MaxEntries)
        {
            this.StoredRecords.RemoveRange(this.MaxEntries, this.StoredRecords.Count - this.MaxEntries);
        }
    }

    public void MigrateFromLegacyRecords()
    {
        if (this.Version < 9 && this.ShowOnlySelfLoot && this.DefaultQuickFilter == LootHistoryQuickFilter.All)
        {
            this.DefaultQuickFilter = LootHistoryQuickFilter.Self;
        }

        if (this.BlacklistedItemIds.Count > 0)
        {
            foreach (var itemId in this.BlacklistedItemIds)
            {
                this.BlacklistedItemKeys.Add($"item:{itemId}");
            }

            this.BlacklistedItemIds.Clear();
        }

        if (this.FavoriteItemIds.Count > 0)
        {
            foreach (var itemId in this.FavoriteItemIds)
            {
                this.FavoriteItemKeys.Add($"item:{itemId}");
            }

            this.FavoriteItemIds.Clear();
        }

#pragma warning disable CS0618
        foreach (var record in this.StoredRecords)
        {
            if (!string.IsNullOrWhiteSpace(record.PlayerName) && string.IsNullOrWhiteSpace(record.WhoName))
            {
                record.WhoName = NormalizeNullable(record.PlayerName);
            }

            if (!string.IsNullOrWhiteSpace(record.ItemText) && string.IsNullOrWhiteSpace(record.LootText))
            {
                record.LootText = NormalizeNullable(record.ItemText);
            }

            record.PlayerName = null;
            record.ItemText = null;

            record.Normalize();
        }
#pragma warning restore CS0618
    }

    private static string? NormalizeNullable(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
