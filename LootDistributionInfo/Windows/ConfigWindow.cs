using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace LootDistributionInfo.Windows;

public sealed class ConfigWindow : Window, IDisposable
{
    private static readonly LootHistoryQuickFilter[] QuickFilters =
    [
        LootHistoryQuickFilter.All,
        LootHistoryQuickFilter.Self,
        LootHistoryQuickFilter.Dungeon,
        LootHistoryQuickFilter.Raid,
        LootHistoryQuickFilter.Favorites,
    ];

    private static readonly LootHistoryGroupingMode[] GroupingModes =
    [
        LootHistoryGroupingMode.Flat,
        LootHistoryGroupingMode.ByZone,
        LootHistoryGroupingMode.ByItem,
        LootHistoryGroupingMode.ByRecipient,
    ];

    private static readonly LootHistorySortMode[] SortModes =
    [
        LootHistorySortMode.NewestFirst,
        LootHistorySortMode.OldestFirst,
        LootHistorySortMode.QuantityHighToLow,
        LootHistorySortMode.ItemName,
        LootHistorySortMode.RecipientName,
    ];

    private readonly Configuration configuration;
    private readonly LootCaptureService lootCaptureService;
    private readonly Action openDebugUi;

    public ConfigWindow(Configuration configuration, LootCaptureService lootCaptureService, Action openDebugUi)
        : base("Loot History Settings")
    {
        this.configuration = configuration;
        this.lootCaptureService = lootCaptureService;
        this.openDebugUi = openDebugUi;
        this.Size = new Vector2(620, 560);
        this.SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        var retainHistory = this.configuration.RetainHistoryBetweenSessions;
        var maxEntries = this.configuration.MaxEntries;
        var debugModeEnabled = this.configuration.DebugModeEnabled;
        var showItemIcons = this.configuration.ShowItemIcons;
        var showItemTooltips = this.configuration.ShowItemTooltips;
        var useCompactMainWindowByDefault = this.configuration.UseCompactMainWindowByDefault;
        var defaultQuickFilter = this.configuration.DefaultQuickFilter;
        var defaultGroupingMode = this.configuration.DefaultGroupingMode;
        var defaultSortMode = this.configuration.DefaultSortMode;
        var changed = false;

        if (ImGui.Checkbox("Save history between sessions", ref retainHistory))
        {
            this.configuration.RetainHistoryBetweenSessions = retainHistory;
            changed = true;
        }

        if (ImGui.DragInt("History size", ref maxEntries, 1f, 1, 5000))
        {
            this.configuration.MaxEntries = maxEntries;
            changed = true;
        }

        if (ImGui.Checkbox("Show debug tools", ref debugModeEnabled))
        {
            this.configuration.DebugModeEnabled = debugModeEnabled;
            changed = true;
        }

        if (ImGui.Checkbox("Show item icons", ref showItemIcons))
        {
            this.configuration.ShowItemIcons = showItemIcons;
            changed = true;
        }

        if (ImGui.Checkbox("Show item tooltips", ref showItemTooltips))
        {
            this.configuration.ShowItemTooltips = showItemTooltips;
            changed = true;
        }

        if (ImGui.Checkbox("Use compact main window by default", ref useCompactMainWindowByDefault))
        {
            this.configuration.UseCompactMainWindowByDefault = useCompactMainWindowByDefault;
            changed = true;
        }

        if (DrawEnumCombo("Default quick filter", QuickFilters, ref defaultQuickFilter, LootHistoryBrowser.GetQuickFilterLabel))
        {
            this.configuration.DefaultQuickFilter = defaultQuickFilter;
            changed = true;
        }

        if (DrawEnumCombo("Default grouping", GroupingModes, ref defaultGroupingMode, LootHistoryBrowser.GetGroupingLabel))
        {
            this.configuration.DefaultGroupingMode = defaultGroupingMode;
            changed = true;
        }

        if (DrawEnumCombo("Default sort", SortModes, ref defaultSortMode, LootHistoryBrowser.GetSortLabel))
        {
            this.configuration.DefaultSortMode = defaultSortMode;
            changed = true;
        }

        ImGui.Spacing();
        ImGui.TextWrapped("Your loot history can stay available between sessions. Turn this off if you only want to keep the current play session.");
        ImGui.TextWrapped("Compact mode shows only who got the loot, how much they got, and the loot name.");
        ImGui.TextDisabled($"Favorite items saved: {this.configuration.FavoriteItemIds.Count}");

        ImGui.Spacing();
        changed |= this.DrawColumnVisibilitySection();

        if (changed)
        {
            this.lootCaptureService.ApplyConfigurationChanges();
        }

        if (this.configuration.DebugModeEnabled)
        {
            ImGui.Spacing();
            if (ImGui.Button("Open debug log"))
            {
                this.openDebugUi();
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextUnformatted("Blacklisted items");

        var blacklistedItems = this.configuration.BlacklistedItemIds
            .Distinct()
            .OrderBy(itemId => itemId)
            .Select(itemId => new
            {
                ItemId = itemId,
                Label = this.lootCaptureService.Records
                    .FirstOrDefault(record => record.ItemId == itemId)?
                    .ResolvedItemName
                    ?? this.lootCaptureService.Records.FirstOrDefault(record => record.ItemId == itemId)?.ItemName
                    ?? $"Item #{itemId}",
            })
            .ToList();

        if (blacklistedItems.Count == 0)
        {
            ImGui.TextDisabled("No blacklisted items yet.");
        }
        else
        {
            foreach (var item in blacklistedItems)
            {
                ImGui.PushID((int)item.ItemId);
                ImGui.TextUnformatted($"{item.Label} ({item.ItemId})");
                ImGui.SameLine();
                if (ImGui.Button("Remove"))
                {
                    this.configuration.BlacklistedItemIds.Remove(item.ItemId);
                    this.configuration.Save();
                }

                ImGui.PopID();
            }
        }
    }

    private static bool DrawEnumCombo<TEnum>(string label, IReadOnlyList<TEnum> values, ref TEnum currentValue, Func<TEnum, string> labelSelector)
        where TEnum : struct, Enum
    {
        var changed = false;
        if (ImGui.BeginCombo(label, labelSelector(currentValue)))
        {
            foreach (var value in values)
            {
                var selected = EqualityComparer<TEnum>.Default.Equals(value, currentValue);
                if (ImGui.Selectable(labelSelector(value), selected))
                {
                    currentValue = value;
                    changed = true;
                }

                if (selected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }

            ImGui.EndCombo();
        }

        return changed;
    }

    private bool DrawColumnVisibilitySection()
    {
        var changed = false;

        if (ImGui.CollapsingHeader("Loot History Columns", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= DrawColumnCheckbox("Time", this.configuration.LootHistoryColumns.ShowTime, value => this.configuration.LootHistoryColumns.ShowTime = value);
            changed |= DrawColumnCheckbox("Zone", this.configuration.LootHistoryColumns.ShowZone, value => this.configuration.LootHistoryColumns.ShowZone = value);
            changed |= DrawColumnCheckbox("Who", this.configuration.LootHistoryColumns.ShowWho, value => this.configuration.LootHistoryColumns.ShowWho = value);
            changed |= DrawColumnCheckbox("Group", this.configuration.LootHistoryColumns.ShowGroup, value => this.configuration.LootHistoryColumns.ShowGroup = value);
            changed |= DrawColumnCheckbox("Quantity", this.configuration.LootHistoryColumns.ShowQuantity, value => this.configuration.LootHistoryColumns.ShowQuantity = value);
            changed |= DrawColumnCheckbox("Icon", this.configuration.LootHistoryColumns.ShowIcon, value => this.configuration.LootHistoryColumns.ShowIcon = value);
            changed |= DrawColumnCheckbox("Loot", this.configuration.LootHistoryColumns.ShowLoot, value => this.configuration.LootHistoryColumns.ShowLoot = value);
            changed |= DrawColumnCheckbox("Raw Line", this.configuration.LootHistoryColumns.ShowRawLine, value => this.configuration.LootHistoryColumns.ShowRawLine = value);
            changed |= DrawColumnCheckbox("Copy", this.configuration.LootHistoryColumns.ShowCopy, value => this.configuration.LootHistoryColumns.ShowCopy = value);
        }

        if (ImGui.CollapsingHeader("Item Details Columns", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= DrawColumnCheckbox("Time", this.configuration.ItemDetailsColumns.ShowTime, value => this.configuration.ItemDetailsColumns.ShowTime = value);
            changed |= DrawColumnCheckbox("Zone", this.configuration.ItemDetailsColumns.ShowZone, value => this.configuration.ItemDetailsColumns.ShowZone = value);
            changed |= DrawColumnCheckbox("Who", this.configuration.ItemDetailsColumns.ShowWho, value => this.configuration.ItemDetailsColumns.ShowWho = value);
            changed |= DrawColumnCheckbox("Group", this.configuration.ItemDetailsColumns.ShowGroup, value => this.configuration.ItemDetailsColumns.ShowGroup = value);
            changed |= DrawColumnCheckbox("Quantity", this.configuration.ItemDetailsColumns.ShowQuantity, value => this.configuration.ItemDetailsColumns.ShowQuantity = value);
            changed |= DrawColumnCheckbox("Icon", this.configuration.ItemDetailsColumns.ShowIcon, value => this.configuration.ItemDetailsColumns.ShowIcon = value);
            changed |= DrawColumnCheckbox("Loot", this.configuration.ItemDetailsColumns.ShowLoot, value => this.configuration.ItemDetailsColumns.ShowLoot = value);
            changed |= DrawColumnCheckbox("Category", this.configuration.ItemDetailsColumns.ShowCategory, value => this.configuration.ItemDetailsColumns.ShowCategory = value);
            changed |= DrawColumnCheckbox("Filter Group", this.configuration.ItemDetailsColumns.ShowFilterGroup, value => this.configuration.ItemDetailsColumns.ShowFilterGroup = value);
            changed |= DrawColumnCheckbox("Equip Slot", this.configuration.ItemDetailsColumns.ShowEquipSlot, value => this.configuration.ItemDetailsColumns.ShowEquipSlot = value);
            changed |= DrawColumnCheckbox("UI Category", this.configuration.ItemDetailsColumns.ShowUiCategory, value => this.configuration.ItemDetailsColumns.ShowUiCategory = value);
            changed |= DrawColumnCheckbox("Search Category", this.configuration.ItemDetailsColumns.ShowSearchCategory, value => this.configuration.ItemDetailsColumns.ShowSearchCategory = value);
            changed |= DrawColumnCheckbox("Sort Category", this.configuration.ItemDetailsColumns.ShowSortCategory, value => this.configuration.ItemDetailsColumns.ShowSortCategory = value);
            changed |= DrawColumnCheckbox("Raw Line", this.configuration.ItemDetailsColumns.ShowRawLine, value => this.configuration.ItemDetailsColumns.ShowRawLine = value);
            changed |= DrawColumnCheckbox("Copy", this.configuration.ItemDetailsColumns.ShowCopy, value => this.configuration.ItemDetailsColumns.ShowCopy = value);
        }

        return changed;

        static bool DrawColumnCheckbox(string label, bool currentValue, Action<bool> setter)
        {
            var value = currentValue;
            if (ImGui.Checkbox(label, ref value))
            {
                setter(value);
                return true;
            }

            return false;
        }
    }
}
