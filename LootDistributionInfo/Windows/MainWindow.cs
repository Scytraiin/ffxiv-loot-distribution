using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;

namespace LootDistributionInfo.Windows;

public sealed class MainWindow : Window, IDisposable
{
    private enum HistoryClearScope
    {
        Everything = 0,
        Zone = 1,
        Character = 2,
    }

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
        LootHistoryGroupingMode.ByDay,
    ];

    private static readonly LootHistorySortMode[] SortModes =
    [
        LootHistorySortMode.NewestFirst,
        LootHistorySortMode.OldestFirst,
        LootHistorySortMode.QuantityHighToLow,
        LootHistorySortMode.ItemName,
        LootHistorySortMode.RecipientName,
    ];

    private static readonly LootRecipientFilter[] RecipientFilters =
    [
        LootRecipientFilter.All,
        LootRecipientFilter.Self,
        LootRecipientFilter.PartyAlliance,
        LootRecipientFilter.Other,
    ];

    private static readonly Vector4 HeaderAccentColor = new(0.91f, 0.83f, 0.56f, 1.0f);
    private static readonly Vector4 SubtleAccentColor = new(0.76f, 0.70f, 0.55f, 1.0f);
    private static readonly Vector4 MutedTextColor = new(0.69f, 0.69f, 0.67f, 1.0f);
    private static readonly Vector4 PanelBorderColor = new(0.38f, 0.33f, 0.24f, 1.0f);
    private static readonly Vector4 ToolbarBackgroundColor = new(0.08f, 0.08f, 0.07f, 1.0f);
    private static readonly Vector4 CardBackgroundColor = new(0.11f, 0.10f, 0.09f, 1.0f);
    private static readonly Vector4 RowBackgroundColor = new(0.17f, 0.16f, 0.14f, 1.0f);
    private static readonly Vector4 ExpandedRowBackgroundColor = new(0.21f, 0.19f, 0.16f, 1.0f);
    private static readonly Vector4 BadgeBackgroundColor = new(0.25f, 0.21f, 0.14f, 1.0f);
    private static readonly Vector4 SelectedChipBackgroundColor = new(0.34f, 0.27f, 0.14f, 1.0f);
    private static readonly Vector4 SelectedChipHoverColor = new(0.42f, 0.33f, 0.18f, 1.0f);
    private static readonly Vector4 SelectedChipActiveColor = new(0.47f, 0.37f, 0.21f, 1.0f);
    private static readonly Vector4 NeutralChipBackgroundColor = new(0.15f, 0.15f, 0.13f, 1.0f);
    private static readonly Vector4 NeutralChipHoverColor = new(0.21f, 0.21f, 0.18f, 1.0f);
    private static readonly Vector4 NeutralChipActiveColor = new(0.25f, 0.25f, 0.21f, 1.0f);
    private static readonly Vector4 SelfAccentColor = new(0.91f, 0.83f, 0.56f, 1.0f);
    private static readonly Vector4 PartyAccentColor = new(0.40f, 0.80f, 1.0f, 1.0f);
    private static readonly Vector4 OtherAccentColor = new(0.88f, 0.88f, 0.85f, 1.0f);

    private readonly LootCaptureService lootCaptureService;
    private readonly Configuration configuration;
    private readonly ITextureProvider textureProvider;
    private readonly Action openConfigUi;
    private readonly Action openDebugUi;
    private readonly HashSet<string> expandedRowKeys = [];
    private bool? lastAppliedCompactMode;
    private string filterText = string.Empty;
    private LootHistoryQuickFilter selectedQuickFilter;
    private LootHistoryGroupingMode selectedGroupingMode;
    private LootHistorySortMode selectedSortMode;
    private LootRecipientFilter selectedRecipientFilter;
    private string selectedCategoryFilter = string.Empty;
    private string selectedZoneFilter = string.Empty;
    private HistoryClearScope clearScope = HistoryClearScope.Everything;
    private string clearSelectedZone = string.Empty;
    private string clearSelectedRecipient = string.Empty;

    public MainWindow(
        LootCaptureService lootCaptureService,
        Configuration configuration,
        ITextureProvider textureProvider,
        Action openConfigUi,
        Action openDebugUi)
        : base("Loot History")
    {
        this.lootCaptureService = lootCaptureService;
        this.configuration = configuration;
        this.textureProvider = textureProvider;
        this.openConfigUi = openConfigUi;
        this.openDebugUi = openDebugUi;
        this.selectedQuickFilter = configuration.DefaultQuickFilter;
        this.selectedGroupingMode = configuration.DefaultGroupingMode;
        this.selectedSortMode = configuration.DefaultSortMode;

        this.ApplyWindowLayout(this.configuration.UseCompactMainWindowByDefault, ImGuiCond.FirstUseEver);
    }

    public void Dispose()
    {
    }

    public override void OnOpen()
    {
        this.ApplyWindowLayout(this.configuration.UseCompactMainWindowByDefault, ImGuiCond.Always);
    }

    public override void Update()
    {
        var compactMode = this.configuration.UseCompactMainWindowByDefault;
        if (this.lastAppliedCompactMode != compactMode)
        {
            this.ApplyWindowLayout(compactMode, ImGuiCond.Always);
        }
    }

    public override void Draw()
    {
        var compactMode = this.configuration.UseCompactMainWindowByDefault;
        var allRecords = this.lootCaptureService.Records.ToList();

        var nonBlacklistedRecords = allRecords
            .Where(record => !this.IsBlacklisted(record))
            .ToList();

        if (compactMode)
        {
            this.DrawCompactView(nonBlacklistedRecords);
        }
        else
        {
            this.DrawFullView(nonBlacklistedRecords);
        }

        this.DrawClearHistoryModal(allRecords);
    }

    private void DrawCompactView(IReadOnlyList<LootRecord> nonBlacklistedRecords)
    {
        this.DrawCompactHeader(nonBlacklistedRecords.Count);
        ImGui.Spacing();

        if (nonBlacklistedRecords.Count == 0)
        {
            ImGui.TextUnformatted("No loot recorded yet.");
            return;
        }

        var visibleRecords = LootHistoryBrowser.FilterAndSort(
            nonBlacklistedRecords,
            new LootHistoryBrowseOptions(
                this.filterText,
                LootHistoryQuickFilter.All,
                LootHistorySortMode.NewestFirst,
                LootRecipientFilter.All,
                string.Empty,
                string.Empty,
                this.configuration.HiddenCategoryLabels,
                this.configuration.FavoriteItemIds));

        if (visibleRecords.Count == 0)
        {
            ImGui.TextUnformatted("No loot matches the current search.");
            return;
        }

        this.DrawCompactTable(visibleRecords);
    }

    private void DrawFullView(IReadOnlyList<LootRecord> nonBlacklistedRecords)
    {
        var categoryFilters = nonBlacklistedRecords
            .Select(record => record.ItemCategoryLabel ?? "Unknown")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var zoneFilters = nonBlacklistedRecords
            .Select(record => string.IsNullOrWhiteSpace(record.ZoneName) ? "Unknown" : record.ZoneName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var visibleRecords = LootHistoryBrowser.FilterAndSort(nonBlacklistedRecords, this.BuildBrowseOptions());
        var groupedRecords = LootHistoryBrowser.Group(visibleRecords, this.selectedGroupingMode);

        // Full mode is intentionally treated as a browser, not just a table: the toolbar defines
        // the visible slice, then tabs decide how that same slice is rendered.
        this.DrawHeaderStrip(nonBlacklistedRecords.Count, visibleRecords.Count);
        ImGui.Spacing();
        this.DrawBrowserToolbar(categoryFilters, zoneFilters);
        ImGui.Spacing();

        if (nonBlacklistedRecords.Count == 0)
        {
            ImGui.TextUnformatted("No loot recorded yet.");
            return;
        }

        if (visibleRecords.Count == 0)
        {
            ImGui.TextUnformatted("No loot matches the current filters.");
            return;
        }

        ImGui.PushStyleColor(ImGuiCol.Tab, NeutralChipBackgroundColor);
        ImGui.PushStyleColor(ImGuiCol.TabHovered, SelectedChipHoverColor);
        ImGui.PushStyleColor(ImGuiCol.TabActive, SelectedChipBackgroundColor);
        ImGui.PushStyleColor(ImGuiCol.TabUnfocused, NeutralChipBackgroundColor);
        ImGui.PushStyleColor(ImGuiCol.TabUnfocusedActive, SelectedChipActiveColor);

        if (!ImGui.BeginTabBar("##loot-history-tabs"))
        {
            ImGui.PopStyleColor(5);
            return;
        }

        if (ImGui.BeginTabItem("Loot History"))
        {
            this.DrawHistoryBrowser(groupedRecords);
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Item Details"))
        {
            this.DrawConfiguredTable("##loot-item-details", this.BuildItemDetailsColumns(), visibleRecords);
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Overview"))
        {
            this.DrawOverviewTab(visibleRecords);
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
        ImGui.PopStyleColor(5);
    }

    private void ApplyWindowLayout(bool compactMode, ImGuiCond sizeCondition)
    {
        this.lastAppliedCompactMode = compactMode;
        this.SizeCondition = sizeCondition;

        if (compactMode)
        {
            this.Size = new Vector2(560, 340);
            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(420, 220),
                MaximumSize = new Vector2(900, 620),
            };
        }
        else
        {
            this.Size = new Vector2(1180, 620);
            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(920, 400),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
            };
        }
    }

    private LootHistoryBrowseOptions BuildBrowseOptions()
    {
        return new LootHistoryBrowseOptions(
            this.filterText,
            this.selectedQuickFilter,
            this.selectedSortMode,
            this.selectedRecipientFilter,
            this.selectedCategoryFilter,
            this.selectedZoneFilter,
            this.configuration.HiddenCategoryLabels,
            this.configuration.FavoriteItemIds);
    }

    private void DrawCompactHeader(int totalEntries)
    {
        ImGui.PushStyleColor(ImGuiCol.ChildBg, ToolbarBackgroundColor);
        ImGui.PushStyleColor(ImGuiCol.Border, PanelBorderColor);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 6f);
        ImGui.BeginChild("##compact-toolbar", new Vector2(-1, 70f), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        ImGui.TextColored(HeaderAccentColor, "Loot History");
        ImGui.SameLine();
        ImGui.TextDisabled($"Compact view • {totalEntries} entries");

        ImGui.SetNextItemWidth(220f);
        ImGui.InputTextWithHint("##compact-loot-filter", "Search loot...", ref this.filterText, 256);
        ImGui.SameLine();
        if (ImGui.Button("Clear"))
        {
            this.OpenClearHistoryModal();
        }

        ImGui.SameLine();
        if (ImGui.Button("Settings"))
        {
            this.openConfigUi();
        }

        ImGui.EndChild();
        ImGui.PopStyleVar();
        ImGui.PopStyleColor(2);
    }

    private void DrawHeaderStrip(int totalEntries, int visibleEntries)
    {
        ImGui.PushStyleColor(ImGuiCol.ChildBg, ToolbarBackgroundColor);
        ImGui.PushStyleColor(ImGuiCol.Border, PanelBorderColor);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 8f);
        ImGui.BeginChild("##history-header", new Vector2(-1, 68f), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        ImGui.TextColored(HeaderAccentColor, "Loot History");
        ImGui.TextDisabled("A curated loot browser with grouping, sorting, favorites, and detailed item context.");

        var statsLabel = visibleEntries == totalEntries
            ? $"{totalEntries} visible entries"
            : $"{visibleEntries} of {totalEntries} visible";

        var statsSize = ImGui.CalcTextSize(statsLabel);
        ImGui.SameLine(MathF.Max(0f, ImGui.GetContentRegionAvail().X - statsSize.X));
        ImGui.TextColored(SubtleAccentColor, statsLabel);
        ImGui.EndChild();
        ImGui.PopStyleVar();
        ImGui.PopStyleColor(2);
    }

    private void DrawBrowserToolbar(IReadOnlyList<string> categoryFilters, IReadOnlyList<string> zoneFilters)
    {
        ImGui.PushStyleColor(ImGuiCol.ChildBg, CardBackgroundColor);
        ImGui.PushStyleColor(ImGuiCol.Border, PanelBorderColor);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 8f);
        ImGui.BeginChild("##history-toolbar", new Vector2(-1, 114f), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        ImGui.TextColored(SubtleAccentColor, "Quick Filters");
        ImGui.SameLine();
        foreach (var quickFilter in QuickFilters)
        {
            this.DrawQuickFilterChip(quickFilter);
            ImGui.SameLine();
        }

        ImGui.NewLine();
        ImGui.Spacing();

        ImGui.SetNextItemWidth(230f);
        ImGui.InputTextWithHint("##loot-filter", "Search loot history...", ref this.filterText, 256);
        ImGui.SameLine();
        this.DrawEnumCombo("##grouping-mode", GroupingModes, ref this.selectedGroupingMode, LootHistoryBrowser.GetGroupingLabel, 118f, value =>
        {
            this.configuration.DefaultGroupingMode = value;
            this.configuration.Save();
        });
        ImGui.SameLine();
        this.DrawEnumCombo("##sort-mode", SortModes, ref this.selectedSortMode, LootHistoryBrowser.GetSortLabel, 145f, value =>
        {
            this.configuration.DefaultSortMode = value;
            this.configuration.Save();
        });
        ImGui.SameLine();
        this.DrawEnumCombo("##recipient-filter", RecipientFilters, ref this.selectedRecipientFilter, GetRecipientFilterLabel, 130f, _ => { });
        ImGui.SameLine();
        this.DrawStringFilterCombo("##category-filter", "All categories", categoryFilters, ref this.selectedCategoryFilter, 150f);
        ImGui.SameLine();
        this.DrawStringFilterCombo("##zone-filter", "All zones", zoneFilters, ref this.selectedZoneFilter, 145f);
        ImGui.SameLine();
        if (ImGui.Button("Clear history"))
        {
            this.OpenClearHistoryModal();
        }

        ImGui.SameLine();
        if (ImGui.Button("Settings"))
        {
            this.openConfigUi();
        }

        if (this.lootCaptureService.DebugModeEnabled)
        {
            ImGui.SameLine();
            if (ImGui.Button("Open debug log"))
            {
                this.openDebugUi();
            }
        }

        ImGui.EndChild();
        ImGui.PopStyleVar();
        ImGui.PopStyleColor(2);
    }

    private void DrawQuickFilterChip(LootHistoryQuickFilter quickFilter)
    {
        var selected = this.selectedQuickFilter == quickFilter;
        ImGui.PushStyleColor(ImGuiCol.Button, selected ? SelectedChipBackgroundColor : NeutralChipBackgroundColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, selected ? SelectedChipHoverColor : NeutralChipHoverColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, selected ? SelectedChipActiveColor : NeutralChipActiveColor);
        ImGui.PushStyleColor(ImGuiCol.Text, selected ? HeaderAccentColor : MutedTextColor);

        if (ImGui.SmallButton(LootHistoryBrowser.GetQuickFilterLabel(quickFilter)))
        {
            this.selectedQuickFilter = quickFilter;
            this.configuration.DefaultQuickFilter = quickFilter;
            this.configuration.Save();
        }

        ImGui.PopStyleColor(4);
    }

    private void DrawHistoryBrowser(IReadOnlyList<LootHistoryGroup> groups)
    {
        if (groups.Count == 0)
        {
            ImGui.TextDisabled("No visible entries.");
            return;
        }

        foreach (var group in groups)
        {
            var showHeader = this.selectedGroupingMode != LootHistoryGroupingMode.Flat;
            if (showHeader)
            {
                ImGui.PushStyleColor(ImGuiCol.Header, NeutralChipBackgroundColor);
                ImGui.PushStyleColor(ImGuiCol.HeaderHovered, NeutralChipHoverColor);
                ImGui.PushStyleColor(ImGuiCol.HeaderActive, NeutralChipActiveColor);
                var open = ImGui.CollapsingHeader($"{group.Label} ({group.Records.Count})", ImGuiTreeNodeFlags.DefaultOpen);
                ImGui.PopStyleColor(3);
                if (!open)
                {
                    continue;
                }
            }

            foreach (var record in group.Records)
            {
                this.DrawBrowserRow(record);
            }

            if (showHeader)
            {
                ImGui.Spacing();
            }
        }
    }

    private void DrawBrowserRow(LootRecord record)
    {
        var rowKey = this.GetRecordKey(record);
        var expanded = this.expandedRowKeys.Contains(rowKey);
        var rowHeight = expanded ? 154f : 62f;
        var backgroundColor = expanded ? ExpandedRowBackgroundColor : RowBackgroundColor;

        // Each row behaves like a compact card with an optional drawer so the default history view
        // stays scannable while still exposing full metadata and actions on demand.
        ImGui.PushID(rowKey);
        ImGui.PushStyleColor(ImGuiCol.ChildBg, backgroundColor);
        ImGui.PushStyleColor(ImGuiCol.Border, PanelBorderColor);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 7f);
        ImGui.BeginChild("##history-row", new Vector2(-1, rowHeight), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        this.DrawRowAccentBar(record);

        if (ImGui.SmallButton(expanded ? "-" : "+"))
        {
            this.ToggleExpandedRow(rowKey);
            expanded = !expanded;
        }

        if (this.configuration.ShowItemIcons && this.configuration.LootHistoryColumns.ShowIcon)
        {
            ImGui.SameLine();
            this.DrawInlineIcon(record, 22f);
        }

        if (this.configuration.LootHistoryColumns.ShowQuantity)
        {
            ImGui.SameLine();
            this.DrawQuantityBadge(record.Quantity);
        }

        if (this.IsFavorite(record))
        {
            ImGui.SameLine();
            ImGui.TextColored(HeaderAccentColor, "Pinned");
        }

        if (this.configuration.LootHistoryColumns.ShowLoot)
        {
            ImGui.SameLine();
            this.DrawLootCell(record, "browser-loot");
        }

        ImGui.Spacing();

        if (this.configuration.LootHistoryColumns.ShowWho)
        {
            this.DrawInlineMetaValue("Who", GetWhoLabel(record), GetGroupColor(record.WhoConfidence));
        }

        if (this.configuration.LootHistoryColumns.ShowGroup)
        {
            this.DrawInlineMetaValue("Group", GetGroupLabel(record.WhoConfidence), GetGroupColor(record.WhoConfidence));
        }

        if (this.configuration.LootHistoryColumns.ShowZone)
        {
            this.DrawInlineMetaValue("Zone", GetDisplayOrUnknown(record.ZoneName), MutedTextColor);
        }

        if (this.configuration.LootHistoryColumns.ShowTime)
        {
            this.DrawInlineMetaValue("When", record.CapturedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"), MutedTextColor);
        }

        if (expanded)
        {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            this.DrawDetailDrawer(record);
        }

        ImGui.EndChild();
        this.DrawContextMenu(record, "browser-row");
        ImGui.PopStyleVar();
        ImGui.PopStyleColor(2);
        ImGui.PopID();
        ImGui.Spacing();
    }

    private void DrawInlineMetaValue(string label, string value, Vector4 valueColor)
    {
        ImGui.TextDisabled($"{label}:");
        ImGui.SameLine(0f, 4f);
        ImGui.TextColored(valueColor, value);
        ImGui.SameLine(0f, 18f);
    }

    private void DrawQuantityBadge(int quantity)
    {
        var backgroundColor = GetQuantityBadgeBackgroundColor(quantity);
        var textColor = GetQuantityBadgeTextColor(quantity);
        ImGui.PushStyleColor(ImGuiCol.Button, backgroundColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, backgroundColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, backgroundColor);
        ImGui.PushStyleColor(ImGuiCol.Text, textColor);
        ImGui.SmallButton($"x{quantity}");
        ImGui.PopStyleColor(4);
    }

    private void DrawDetailDrawer(LootRecord record)
    {
        if (ImGui.BeginTable("##drawer-details", 2, ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextDisabled("Classification");
            ImGui.TextUnformatted(record.ItemCategoryLabel ?? "Unknown");
            ImGui.TextDisabled("Filter Group");
            ImGui.TextUnformatted(FormatLabelWithId(record.FilterGroupLabel, record.FilterGroupId));
            ImGui.TextDisabled("Equip Slot");
            ImGui.TextUnformatted(FormatLabelWithId(record.EquipSlotCategoryLabel, record.EquipSlotCategoryId));

            ImGui.TableNextColumn();
            ImGui.TextDisabled("Zone");
            ImGui.TextUnformatted(GetDisplayOrUnknown(record.ZoneName));
            ImGui.TextDisabled("Timestamp");
            ImGui.TextUnformatted(record.CapturedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
            ImGui.TextDisabled("Loot Type");
            ImGui.TextUnformatted(GetLootTypeLabel(record.LootTypeBucket));
            ImGui.EndTable();
        }

        ImGui.Spacing();
        ImGui.TextDisabled("Raw line");
        ImGui.PushTextWrapPos();
        ImGui.TextUnformatted(record.RawText);
        ImGui.PopTextWrapPos();
        ImGui.Spacing();

        if (ImGui.Button("Copy line"))
        {
            ImGui.SetClipboardText(this.BuildClipboardLine(record));
        }

        ImGui.SameLine();
        this.DrawFavoriteButton(record, detailContext: true);

        ImGui.SameLine();
        this.DrawHideButton(record);
    }

    private void DrawFavoriteButton(LootRecord record, bool detailContext)
    {
        if (record.ItemId is not uint itemId)
        {
            ImGui.BeginDisabled();
            ImGui.Button(detailContext ? "Pin item" : "Pin");
            ImGui.EndDisabled();
            return;
        }

        var isFavorite = this.configuration.FavoriteItemIds.Contains(itemId);
        var label = isFavorite ? (detailContext ? "Unpin item" : "Unpin") : (detailContext ? "Pin item" : "Pin");
        if (ImGui.Button(label))
        {
            if (isFavorite)
            {
                this.configuration.FavoriteItemIds.Remove(itemId);
            }
            else
            {
                this.configuration.FavoriteItemIds.Add(itemId);
            }

            this.configuration.FavoriteItemIds = this.configuration.FavoriteItemIds
                .Distinct()
                .OrderBy(value => value)
                .ToList();
            this.configuration.Save();
        }
    }

    private void DrawHideButton(LootRecord record)
    {
        if (record.ItemId is not uint itemId)
        {
            ImGui.BeginDisabled();
            ImGui.Button("Hide item");
            ImGui.EndDisabled();
            return;
        }

        if (ImGui.Button("Hide item"))
        {
            if (!this.configuration.BlacklistedItemIds.Contains(itemId))
            {
                this.configuration.BlacklistedItemIds.Add(itemId);
                this.configuration.BlacklistedItemIds = this.configuration.BlacklistedItemIds
                    .Distinct()
                    .OrderBy(value => value)
                    .ToList();
                this.configuration.Save();
            }
        }
    }

    private void ToggleExpandedRow(string rowKey)
    {
        if (!this.expandedRowKeys.Add(rowKey))
        {
            this.expandedRowKeys.Remove(rowKey);
        }
    }

    private string GetRecordKey(LootRecord record)
    {
        return $"{record.CapturedAtUtc.ToUnixTimeMilliseconds()}::{GetRecordScopeHash(record)}::{record.Source}";
    }

    private bool IsFavorite(LootRecord record)
    {
        return record.ItemId is uint itemId && this.configuration.FavoriteItemIds.Contains(itemId);
    }

    private void DrawCompactTable(IReadOnlyList<LootRecord> records)
    {
        if (!ImGui.BeginTable("##compact-loot-history", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX, new Vector2(-1, -1)))
        {
            return;
        }

        ImGui.TableSetupColumn("Who", ImGuiTableColumnFlags.WidthFixed, 170f);
        ImGui.TableSetupColumn("Qty", ImGuiTableColumnFlags.WidthFixed, 70f);
        ImGui.TableSetupColumn("Loot", ImGuiTableColumnFlags.WidthStretch, 220f);
        ImGui.TableSetupColumn("Copy", ImGuiTableColumnFlags.WidthFixed, 64f);
        ImGui.TableHeadersRow();

        foreach (var record in records)
        {
            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            this.DrawRecordTextCell(record, GetWhoLabel(record), "compact-who", GetGroupColor(record.WhoConfidence));

            ImGui.TableSetColumnIndex(1);
            this.DrawRecordTextCell(record, record.Quantity.ToString(), "compact-quantity");

            ImGui.TableSetColumnIndex(2);
            this.DrawLootCell(record, "compact-loot");

            ImGui.TableSetColumnIndex(3);
            this.DrawCopyButton(record, "compact");
        }

        ImGui.EndTable();
    }

    private void DrawConfiguredTable(string tableId, IReadOnlyList<TableColumnDefinition> columns, IReadOnlyList<LootRecord> records)
    {
        var totalColumns = columns.Count + (this.lootCaptureService.DebugModeEnabled ? 1 : 0);
        if (columns.Count == 0)
        {
            ImGui.TextDisabled("No columns are enabled for this view.");
            return;
        }

        if (!ImGui.BeginTable(tableId, totalColumns, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX, new Vector2(-1, -1)))
        {
            return;
        }

        foreach (var column in columns)
        {
            ImGui.TableSetupColumn(column.Label, column.Flags, column.Width);
        }

        if (this.lootCaptureService.DebugModeEnabled)
        {
            ImGui.TableSetupColumn("Source", ImGuiTableColumnFlags.WidthFixed, 95f);
        }

        ImGui.TableHeadersRow();

        foreach (var record in records)
        {
            ImGui.TableNextRow();

            for (var index = 0; index < columns.Count; index++)
            {
                ImGui.TableSetColumnIndex(index);
                columns[index].DrawCell(record);
            }

            if (this.lootCaptureService.DebugModeEnabled)
            {
                ImGui.TableSetColumnIndex(columns.Count);
                ImGui.TextUnformatted(record.Source.ToString());
            }
        }

        ImGui.EndTable();
    }

    private IReadOnlyList<TableColumnDefinition> BuildItemDetailsColumns()
    {
        var columns = new List<TableColumnDefinition>();
        var visibility = this.configuration.ItemDetailsColumns;

        if (visibility.ShowTime)
        {
            columns.Add(new TableColumnDefinition("Time", ImGuiTableColumnFlags.WidthFixed, 150f, record => ImGui.TextUnformatted(record.CapturedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"))));
        }

        if (visibility.ShowZone)
        {
            columns.Add(new TableColumnDefinition("Zone", ImGuiTableColumnFlags.WidthFixed, 170f, record => this.DrawRecordTextCell(record, GetDisplayOrUnknown(record.ZoneName), "details-zone")));
        }

        if (visibility.ShowWho)
        {
            columns.Add(new TableColumnDefinition("Who", ImGuiTableColumnFlags.WidthFixed, 190f, record => this.DrawRecordTextCell(record, GetWhoLabel(record), "details-who", GetGroupColor(record.WhoConfidence))));
        }

        if (visibility.ShowGroup)
        {
            columns.Add(new TableColumnDefinition("Group", ImGuiTableColumnFlags.WidthFixed, 120f, record => this.DrawRecordTextCell(record, GetGroupLabel(record.WhoConfidence), "details-group", GetGroupColor(record.WhoConfidence))));
        }

        if (visibility.ShowQuantity)
        {
            columns.Add(new TableColumnDefinition("Qty", ImGuiTableColumnFlags.WidthFixed, 70f, record => this.DrawRecordTextCell(record, record.Quantity.ToString(), "details-quantity")));
        }

        if (visibility.ShowIcon && this.configuration.ShowItemIcons)
        {
            columns.Add(new TableColumnDefinition("Icon", ImGuiTableColumnFlags.WidthFixed, 42f, record => this.DrawIconCell(record, "details")));
        }

        if (visibility.ShowLoot)
        {
            columns.Add(new TableColumnDefinition("Loot", ImGuiTableColumnFlags.WidthStretch, 220f, record => this.DrawLootCell(record, "details")));
        }

        if (visibility.ShowCategory)
        {
            columns.Add(new TableColumnDefinition("Category", ImGuiTableColumnFlags.WidthFixed, 150f, record => this.DrawRecordTextCell(record, record.ItemCategoryLabel ?? "Unknown", "details-category")));
        }

        if (visibility.ShowFilterGroup)
        {
            columns.Add(new TableColumnDefinition("Filter Group", ImGuiTableColumnFlags.WidthFixed, 170f, record => this.DrawRecordTextCell(record, FormatLabelWithId(record.FilterGroupLabel, record.FilterGroupId), "details-filter-group")));
        }

        if (visibility.ShowEquipSlot)
        {
            columns.Add(new TableColumnDefinition("Equip Slot", ImGuiTableColumnFlags.WidthFixed, 170f, record => this.DrawRecordTextCell(record, FormatLabelWithId(record.EquipSlotCategoryLabel, record.EquipSlotCategoryId), "details-equip-slot")));
        }

        if (visibility.ShowUiCategory)
        {
            columns.Add(new TableColumnDefinition("UI Category", ImGuiTableColumnFlags.WidthFixed, 100f, record => this.DrawRecordTextCell(record, FormatOptional(record.ItemUICategoryId), "details-ui-category")));
        }

        if (visibility.ShowSearchCategory)
        {
            columns.Add(new TableColumnDefinition("Search Category", ImGuiTableColumnFlags.WidthFixed, 120f, record => this.DrawRecordTextCell(record, FormatOptional(record.ItemSearchCategoryId), "details-search-category")));
        }

        if (visibility.ShowSortCategory)
        {
            columns.Add(new TableColumnDefinition("Sort Category", ImGuiTableColumnFlags.WidthFixed, 100f, record => this.DrawRecordTextCell(record, FormatOptional(record.ItemSortCategoryId), "details-sort-category")));
        }

        if (visibility.ShowRawLine)
        {
            columns.Add(new TableColumnDefinition("Raw Line", ImGuiTableColumnFlags.WidthStretch, 320f, record => this.DrawRecordTextCell(record, record.RawText, "details-raw")));
        }

        if (visibility.ShowCopy)
        {
            columns.Add(new TableColumnDefinition("Copy", ImGuiTableColumnFlags.WidthFixed, 72f, record => this.DrawCopyButton(record, "details")));
        }

        return columns;
    }

    private void DrawOverviewTab(IReadOnlyList<LootRecord> records)
    {
        var summary = LootOverviewSummary.Build(records);

        ImGui.TextDisabled("Overview reflects the current filters and favorites state.");
        ImGui.Spacing();

        if (ImGui.BeginTable("##overview-summary-cards", 4, ImGuiTableFlags.SizingStretchSame))
        {
            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            this.DrawOverviewCard("Entries", summary.TotalEntries.ToString(), LootHistoryBrowser.GetQuickFilterLabel(this.selectedQuickFilter));

            ImGui.TableSetColumnIndex(1);
            this.DrawOverviewCard("Unique Items", summary.UniqueItems.ToString(), "Distinct visible items");

            ImGui.TableSetColumnIndex(2);
            this.DrawOverviewCard("Latest Loot", summary.LatestItemAtUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "-", "Most recent visible entry");

            ImGui.TableSetColumnIndex(3);
            this.DrawOverviewCard("Categories", summary.TopCategories.Count.ToString(), "Visible category buckets");

            ImGui.EndTable();
        }

        ImGui.Spacing();

        if (ImGui.BeginTable("##overview-sections", 2, ImGuiTableFlags.SizingStretchSame))
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            this.DrawBucketSection("Top Zones", summary.TopZones, showIcons: false);
            ImGui.Spacing();
            this.DrawBucketSection("Top Categories", summary.TopCategories, showIcons: false);

            ImGui.TableNextColumn();
            this.DrawBucketSection("Top Items", summary.TopItems, showIcons: true);
            ImGui.Spacing();
            this.DrawBucketSection("Rarity Breakdown", summary.RarityBreakdown, showIcons: false);

            ImGui.EndTable();
        }
    }

    private void DrawBucketSection(string title, IReadOnlyList<LootOverviewBucket> buckets, bool showIcons)
    {
        ImGui.TextColored(HeaderAccentColor, title);
        ImGui.Separator();

        if (buckets.Count == 0)
        {
            ImGui.TextDisabled("No data available.");
            return;
        }

        foreach (var bucket in buckets)
        {
            if (showIcons)
            {
                this.DrawInlineIcon(bucket.SampleRecord, 18f);
                ImGui.SameLine();
                this.DrawColoredItemText(bucket.SampleRecord, bucket.Label);
            }
            else
            {
                ImGui.TextUnformatted(bucket.Label);
            }

            ImGui.SameLine();
            ImGui.SetCursorPosX(MathF.Max(ImGui.GetCursorPosX(), ImGui.GetWindowWidth() - 90f));
            ImGui.TextDisabled(bucket.Count.ToString());
        }
    }

    private bool IsBlacklisted(LootRecord record)
    {
        return record.ItemId is uint itemId && this.configuration.BlacklistedItemIds.Contains(itemId);
    }

    private void DrawIconCell(LootRecord record, string scope)
    {
        if (!this.configuration.ShowItemIcons || record.IconId is not uint iconId)
        {
            ImGui.TextDisabled("-");
            return;
        }

        var lookup = new GameIconLookup(iconId, record.IsHighQuality);
        var texture = this.textureProvider.GetFromGameIcon(lookup);
        if (texture.TryGetWrap(out var wrap, out _))
        {
            ImGui.Image(wrap.Handle, new Vector2(20, 20));
        }
        else
        {
            ImGui.TextDisabled("?");
        }

        this.DrawTooltipAndContext(record, $"{scope}-icon");
    }

    private void DrawLootCell(LootRecord record, string scope)
    {
        var label = GetDisplayItemName(record);
        if (record.IsHighQuality)
        {
            label = $"{label} HQ";
        }

        var truncated = this.DrawColoredItemText(record, label);
        this.DrawTooltipAndContext(record, $"{scope}-loot", truncated);
    }

    private void DrawRecordTextCell(LootRecord record, string text, string scope, Vector4? color = null)
    {
        var displayText = GetTruncatedDisplayText(text, this.GetAvailableCellTextWidth(), out var truncated);
        if (color is Vector4 textColor)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, textColor);
            ImGui.TextUnformatted(displayText);
            ImGui.PopStyleColor();
        }
        else
        {
            ImGui.TextUnformatted(displayText);
        }

        if (truncated && ImGui.IsItemHovered())
        {
            this.DrawTextTooltip(text);
        }

        this.DrawContextMenu(record, scope);
    }

    private bool DrawColoredItemText(LootRecord record, string label)
    {
        var displayText = GetTruncatedDisplayText(label, this.GetAvailableCellTextWidth(), out var truncated);

        if (record.Rarity is uint rarity)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, GetRarityColor(rarity));
            ImGui.TextUnformatted(displayText);
            ImGui.PopStyleColor();
            return truncated;
        }

        ImGui.TextUnformatted(displayText);
        return truncated;
    }

    private void DrawTooltipAndContext(LootRecord record, string scope, bool textWasTruncated = false)
    {
        if (ImGui.IsItemHovered())
        {
            if (textWasTruncated)
            {
                if (this.configuration.ShowItemTooltips)
                {
                    this.DrawItemTooltip(record);
                }
                else
                {
                    this.DrawTextTooltip(GetDisplayItemName(record));
                }
            }
            else if (this.configuration.ShowItemTooltips && scope.Contains("-icon", StringComparison.Ordinal))
            {
                this.DrawItemTooltip(record);
            }
        }

        this.DrawContextMenu(record, scope);
    }

    private void DrawContextMenu(LootRecord record, string scope)
    {
        if (!ImGui.BeginPopupContextItem($"##record-context-{scope}-{record.CapturedAtUtc.ToUnixTimeMilliseconds()}-{GetRecordScopeHash(record)}"))
        {
            return;
        }

        if (ImGui.MenuItem("Copy line"))
        {
            ImGui.SetClipboardText(this.BuildClipboardLine(record));
        }

        ImGui.Separator();

        if (record.ItemId is uint itemId)
        {
            var isFavorite = this.configuration.FavoriteItemIds.Contains(itemId);
            if (ImGui.MenuItem(isFavorite ? "Unpin item" : "Pin item"))
            {
                if (isFavorite)
                {
                    this.configuration.FavoriteItemIds.Remove(itemId);
                }
                else
                {
                    this.configuration.FavoriteItemIds.Add(itemId);
                }

                this.configuration.FavoriteItemIds = this.configuration.FavoriteItemIds
                    .Distinct()
                    .OrderBy(value => value)
                    .ToList();
                this.configuration.Save();
            }

            if (ImGui.MenuItem($"Hide '{GetDisplayItemName(record)}'"))
            {
                if (!this.configuration.BlacklistedItemIds.Contains(itemId))
                {
                    this.configuration.BlacklistedItemIds.Add(itemId);
                    this.configuration.BlacklistedItemIds = this.configuration.BlacklistedItemIds
                        .Distinct()
                        .OrderBy(value => value)
                        .ToList();
                    this.configuration.Save();
                }
            }
        }
        else
        {
            ImGui.TextDisabled("Pin item unavailable");
            ImGui.TextDisabled("Hide item unavailable");
        }

        ImGui.EndPopup();
    }

    private void DrawItemTooltip(LootRecord record)
    {
        ImGui.BeginTooltip();
        ImGui.TextUnformatted(GetDisplayItemName(record));

        if (record.IsHighQuality)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(1.0f, 0.86f, 0.3f, 1.0f), "HQ");
        }

        ImGui.Separator();
        ImGui.TextUnformatted($"Quantity: {record.Quantity}");
        ImGui.TextUnformatted($"When: {record.CapturedAtUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss}");
        ImGui.TextUnformatted($"Zone: {GetDisplayOrUnknown(record.ZoneName)}");
        ImGui.TextUnformatted($"Who: {GetWhoLabel(record)}");
        ImGui.TextUnformatted($"Group: {GetGroupLabel(record.WhoConfidence)}");
        ImGui.TextUnformatted($"Loot type: {GetLootTypeLabel(record.LootTypeBucket)}");
        ImGui.TextUnformatted($"Category: {record.ItemCategoryLabel ?? "Unknown"}");
        ImGui.TextUnformatted($"Filter Group: {FormatLabelWithId(record.FilterGroupLabel, record.FilterGroupId)}");
        ImGui.TextUnformatted($"Equip Slot: {FormatLabelWithId(record.EquipSlotCategoryLabel, record.EquipSlotCategoryId)}");
        ImGui.Separator();
        ImGui.TextWrapped(record.RawText);
        ImGui.EndTooltip();
    }

    private void DrawTextTooltip(string text)
    {
        ImGui.BeginTooltip();
        ImGui.TextUnformatted(text);
        ImGui.EndTooltip();
    }

    private void DrawCopyButton(LootRecord record, string scope)
    {
        if (ImGui.SmallButton($"Copy##{scope}-{record.CapturedAtUtc.ToUnixTimeMilliseconds()}-{GetRecordScopeHash(record)}"))
        {
            ImGui.SetClipboardText(this.BuildClipboardLine(record));
        }
    }

    private void OpenClearHistoryModal()
    {
        this.clearScope = HistoryClearScope.Everything;
        this.clearSelectedZone = string.Empty;
        this.clearSelectedRecipient = string.Empty;
        ImGui.OpenPopup("Clear history##modal");
    }

    private void DrawClearHistoryModal(IReadOnlyList<LootRecord> allRecords)
    {
        var zoneOptions = allRecords
            .Select(record => string.IsNullOrWhiteSpace(record.ZoneName) ? "Unknown" : record.ZoneName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var recipientOptions = allRecords
            .Select(GetWhoLabel)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (this.clearScope == HistoryClearScope.Zone && zoneOptions.Count > 0 && !zoneOptions.Contains(this.clearSelectedZone, StringComparer.OrdinalIgnoreCase))
        {
            this.clearSelectedZone = zoneOptions[0];
        }

        if (this.clearScope == HistoryClearScope.Character && recipientOptions.Count > 0 && !recipientOptions.Contains(this.clearSelectedRecipient, StringComparer.OrdinalIgnoreCase))
        {
            this.clearSelectedRecipient = recipientOptions[0];
        }

        if (!ImGui.BeginPopupModal("Clear history##modal", ImGuiWindowFlags.AlwaysAutoResize))
        {
            return;
        }

        ImGui.TextWrapped("Choose which stored history entries should be removed.");
        ImGui.Spacing();

        if (ImGui.RadioButton("Everything", this.clearScope == HistoryClearScope.Everything))
        {
            this.clearScope = HistoryClearScope.Everything;
        }

        if (ImGui.RadioButton("Specific zone", this.clearScope == HistoryClearScope.Zone))
        {
            this.clearScope = HistoryClearScope.Zone;
            this.clearSelectedZone = zoneOptions.FirstOrDefault() ?? string.Empty;
        }

        if (ImGui.RadioButton("Specific character", this.clearScope == HistoryClearScope.Character))
        {
            this.clearScope = HistoryClearScope.Character;
            this.clearSelectedRecipient = recipientOptions.FirstOrDefault() ?? string.Empty;
        }

        ImGui.Spacing();

        if (this.clearScope == HistoryClearScope.Zone)
        {
            this.DrawStringFilterCombo("##clear-zone", "Select zone", zoneOptions, ref this.clearSelectedZone, 260f);
        }
        else if (this.clearScope == HistoryClearScope.Character)
        {
            this.DrawStringFilterCombo("##clear-recipient", "Select character", recipientOptions, ref this.clearSelectedRecipient, 260f);
        }

        var selectedCount = this.GetSelectedClearCount(allRecords);
        ImGui.TextDisabled($"{selectedCount} entr{(selectedCount == 1 ? "y" : "ies")} selected.");

        var canClear = selectedCount > 0;
        if (!canClear)
        {
            ImGui.BeginDisabled();
        }

        if (ImGui.Button("Clear selected"))
        {
            switch (this.clearScope)
            {
                case HistoryClearScope.Zone:
                    this.lootCaptureService.ClearHistoryForZone(this.clearSelectedZone);
                    break;
                case HistoryClearScope.Character:
                    this.lootCaptureService.ClearHistoryForRecipient(this.clearSelectedRecipient);
                    break;
                default:
                    this.lootCaptureService.ClearHistory();
                    break;
            }

            this.expandedRowKeys.Clear();
            ImGui.CloseCurrentPopup();
        }

        if (!canClear)
        {
            ImGui.EndDisabled();
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel"))
        {
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    private int GetSelectedClearCount(IReadOnlyList<LootRecord> allRecords)
    {
        return this.clearScope switch
        {
            HistoryClearScope.Zone => allRecords.Count(record => string.Equals(
                string.IsNullOrWhiteSpace(record.ZoneName) ? "Unknown" : record.ZoneName,
                this.clearSelectedZone,
                StringComparison.OrdinalIgnoreCase)),
            HistoryClearScope.Character => allRecords.Count(record => string.Equals(
                GetWhoLabel(record),
                this.clearSelectedRecipient,
                StringComparison.OrdinalIgnoreCase)),
            _ => allRecords.Count,
        };
    }

    private static int GetRecordScopeHash(LootRecord record)
    {
        return HashCode.Combine(record.CapturedAtUtc, record.RawText, record.Source);
    }

    private void DrawInlineIcon(LootRecord record, float size)
    {
        if (!this.configuration.ShowItemIcons || record.IconId is not uint iconId)
        {
            ImGui.TextDisabled("-");
            return;
        }

        var texture = this.textureProvider.GetFromGameIcon(new GameIconLookup(iconId, record.IsHighQuality));
        if (texture.TryGetWrap(out var wrap, out _))
        {
            ImGui.Image(wrap.Handle, new Vector2(size, size));
        }
        else
        {
            ImGui.TextDisabled("?");
        }
    }

    private void DrawRowAccentBar(LootRecord record)
    {
        var drawList = ImGui.GetWindowDrawList();
        var windowPos = ImGui.GetWindowPos();
        var windowSize = ImGui.GetWindowSize();
        var accentColor = GetRowAccentColor(record);
        drawList.AddRectFilled(
            windowPos,
            windowPos + new Vector2(4f, windowSize.Y),
            ImGui.GetColorU32(accentColor));
    }

    private void DrawEnumCombo<TEnum>(string id, IReadOnlyList<TEnum> values, ref TEnum currentValue, Func<TEnum, string> labelSelector, float width, Action<TEnum> onChanged)
        where TEnum : struct, Enum
    {
        ImGui.SetNextItemWidth(width);
        if (!ImGui.BeginCombo(id, labelSelector(currentValue)))
        {
            return;
        }

        foreach (var value in values)
        {
            var selected = EqualityComparer<TEnum>.Default.Equals(value, currentValue);
            if (ImGui.Selectable(labelSelector(value), selected))
            {
                currentValue = value;
                onChanged(value);
            }

            if (selected)
            {
                ImGui.SetItemDefaultFocus();
            }
        }

        ImGui.EndCombo();
    }

    private void DrawStringFilterCombo(string id, string allLabel, IReadOnlyList<string> options, ref string selectedValue, float width)
    {
        var preview = string.IsNullOrEmpty(selectedValue) ? allLabel : selectedValue;
        ImGui.SetNextItemWidth(width);
        if (!ImGui.BeginCombo(id, preview))
        {
            return;
        }

        if (ImGui.Selectable(allLabel, string.IsNullOrEmpty(selectedValue)))
        {
            selectedValue = string.Empty;
        }

        foreach (var option in options)
        {
            var selected = string.Equals(option, selectedValue, StringComparison.OrdinalIgnoreCase);
            if (ImGui.Selectable(option, selected))
            {
                selectedValue = option;
            }

            if (selected)
            {
                ImGui.SetItemDefaultFocus();
            }
        }

        ImGui.EndCombo();
    }

    private static string GetRecipientFilterLabel(LootRecipientFilter filter)
    {
        return filter switch
        {
            LootRecipientFilter.Self => "Self only",
            LootRecipientFilter.PartyAlliance => "Party/Alliance",
            LootRecipientFilter.Other => "Other only",
            _ => "All recipients",
        };
    }

    private static string GetGroupLabel(LootWhoConfidence confidence)
    {
        return confidence switch
        {
            LootWhoConfidence.Self => "Self",
            LootWhoConfidence.PartyOrAllianceVerified => "Party/Alliance",
            _ => "Other",
        };
    }

    private static string GetWhoLabel(LootRecord record)
    {
        return record.WhoDisplayName ?? record.WhoName ?? "Unknown";
    }

    private static string GetDisplayItemName(LootRecord record)
    {
        return LootHistoryBrowser.GetDisplayItemName(record);
    }

    private static string GetLootTypeLabel(LootTypeBucket bucket)
    {
        return bucket switch
        {
            LootTypeBucket.Dungeon => "Dungeon",
            LootTypeBucket.Raid => "Raid",
            _ => "Other",
        };
    }

    private static string FormatLabelWithId(string? label, object? id)
    {
        var formattedId = FormatOptional(id);
        if (string.IsNullOrWhiteSpace(label))
        {
            return string.IsNullOrWhiteSpace(formattedId) ? "Unknown" : formattedId;
        }

        return string.IsNullOrWhiteSpace(formattedId) ? label : $"{label} ({formattedId})";
    }

    private static string GetDisplayOrUnknown(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
    }

    private string BuildClipboardLine(LootRecord record)
    {
        var who = GetWhoLabel(record);
        var loot = GetDisplayItemName(record);
        if (record.IsHighQuality)
        {
            loot = $"{loot} HQ";
        }

        return $"{who};{loot}";
    }

    private static string FormatOptional(object? value)
    {
        return value?.ToString() ?? string.Empty;
    }

    private void DrawOverviewCard(string title, string value, string subtitle)
    {
        ImGui.PushStyleColor(ImGuiCol.ChildBg, CardBackgroundColor);
        ImGui.PushStyleColor(ImGuiCol.Border, PanelBorderColor);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 7f);
        ImGui.BeginChild($"##overview-card-{title}", new Vector2(0, 82), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        ImGui.TextColored(HeaderAccentColor, title);
        ImGui.Spacing();
        ImGui.TextUnformatted(value);
        ImGui.TextDisabled(subtitle);
        ImGui.EndChild();
        ImGui.PopStyleVar();
        ImGui.PopStyleColor(2);
    }

    private float GetAvailableCellTextWidth()
    {
        return MathF.Max(1f, ImGui.GetContentRegionAvail().X);
    }

    private static string GetTruncatedDisplayText(string text, float maxWidth, out bool truncated)
    {
        var normalized = NormalizeInlineText(text);
        if (string.IsNullOrEmpty(normalized))
        {
            truncated = false;
            return string.Empty;
        }

        if (ImGui.CalcTextSize(normalized).X <= maxWidth)
        {
            truncated = false;
            return normalized;
        }

        const string ellipsis = "...";
        var ellipsisWidth = ImGui.CalcTextSize(ellipsis).X;
        if (ellipsisWidth >= maxWidth)
        {
            truncated = true;
            return ellipsis;
        }

        var low = 0;
        var high = normalized.Length;
        while (low < high)
        {
            var mid = (low + high + 1) / 2;
            var candidate = normalized[..mid] + ellipsis;
            if (ImGui.CalcTextSize(candidate).X <= maxWidth)
            {
                low = mid;
            }
            else
            {
                high = mid - 1;
            }
        }

        truncated = true;
        return low <= 0 ? ellipsis : normalized[..low] + ellipsis;
    }

    private static string NormalizeInlineText(string text)
    {
        return text
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();
    }

    private static Vector4 GetRarityColor(uint rarity)
    {
        return rarity switch
        {
            1 => new Vector4(0.80f, 0.80f, 0.80f, 1.0f),
            2 => new Vector4(0.51f, 0.83f, 0.39f, 1.0f),
            3 => new Vector4(0.31f, 0.55f, 0.81f, 1.0f),
            4 => new Vector4(0.65f, 0.51f, 0.81f, 1.0f),
            7 => new Vector4(0.65f, 0.51f, 0.81f, 1.0f),
            _ => new Vector4(0.92f, 0.92f, 0.92f, 1.0f),
        };
    }

    private static Vector4 GetGroupColor(LootWhoConfidence confidence)
    {
        return confidence switch
        {
            LootWhoConfidence.Self => SelfAccentColor,
            LootWhoConfidence.PartyOrAllianceVerified => PartyAccentColor,
            _ => OtherAccentColor,
        };
    }

    private static Vector4 GetRowAccentColor(LootRecord record)
    {
        if (record.Rarity is uint rarity)
        {
            return GetRarityColor(rarity);
        }

        return record.WhoConfidence switch
        {
            LootWhoConfidence.Self => new Vector4(SelfAccentColor.X, SelfAccentColor.Y, SelfAccentColor.Z, 0.85f),
            LootWhoConfidence.PartyOrAllianceVerified => new Vector4(PartyAccentColor.X, PartyAccentColor.Y, PartyAccentColor.Z, 0.85f),
            _ => new Vector4(SubtleAccentColor.X, SubtleAccentColor.Y, SubtleAccentColor.Z, 0.60f),
        };
    }

    private static Vector4 GetQuantityBadgeBackgroundColor(int quantity)
    {
        if (quantity >= 99)
        {
            return new Vector4(0.43f, 0.18f, 0.18f, 1.0f);
        }

        if (quantity >= 10)
        {
            return new Vector4(0.34f, 0.27f, 0.14f, 1.0f);
        }

        if (quantity <= 1)
        {
            return NeutralChipBackgroundColor;
        }

        return BadgeBackgroundColor;
    }

    private static Vector4 GetQuantityBadgeTextColor(int quantity)
    {
        if (quantity >= 99)
        {
            return new Vector4(1.0f, 0.90f, 0.90f, 1.0f);
        }

        if (quantity <= 1)
        {
            return SubtleAccentColor;
        }

        return HeaderAccentColor;
    }

    private sealed record TableColumnDefinition(string Label, ImGuiTableColumnFlags Flags, float Width, Action<LootRecord> DrawCell);
}
