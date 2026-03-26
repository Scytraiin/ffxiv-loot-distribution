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
    private static readonly string[] GroupFilters = ["All groups", "Self", "Party/Alliance", "Other"];
    private static readonly string[] LootTypeFilters = ["All loot", "Dungeon loot", "Raid loot"];

    private readonly LootCaptureService lootCaptureService;
    private readonly Configuration configuration;
    private readonly ITextureProvider textureProvider;
    private readonly Action openConfigUi;
    private readonly Action openDebugUi;
    private string filterText = string.Empty;
    private int selectedLootTypeFilter;
    private int selectedGroupFilter;
    private string selectedCategoryFilter = string.Empty;
    private string selectedZoneFilter = string.Empty;

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

        this.SizeCondition = ImGuiCond.FirstUseEver;
        if (this.configuration.UseCompactMainWindowByDefault)
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
            this.Size = new Vector2(1180, 520);
            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(900, 320),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
            };
        }
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        var compactMode = this.configuration.UseCompactMainWindowByDefault;
        this.ApplyWindowLayout(compactMode);

        var nonBlacklistedRecords = this.lootCaptureService.Records
            .Where(record => !this.IsBlacklisted(record))
            .ToList();

        var visibleRecords = nonBlacklistedRecords
            .Where(this.RecordMatchesCurrentFilters)
            .ToList();

        if (compactMode)
        {
            this.DrawCompactToolbar();
            ImGui.Spacing();

            if (nonBlacklistedRecords.Count == 0)
            {
                ImGui.TextUnformatted("No loot recorded yet.");
                return;
            }

            if (visibleRecords.Count == 0)
            {
                ImGui.TextUnformatted("No loot matches the current search.");
                return;
            }

            this.DrawCompactTable(visibleRecords);
            return;
        }

        ImGui.TextWrapped("Tracks loot messages and keeps a searchable history of where the loot happened and who received it.");
        ImGui.Separator();

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

        this.DrawFullFilters(categoryFilters, zoneFilters);
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

        if (!ImGui.BeginTabBar("##loot-history-tabs"))
        {
            return;
        }

        if (ImGui.BeginTabItem("Loot History"))
        {
            this.DrawConfiguredTable("##loot-history", this.BuildLootHistoryColumns(), visibleRecords);
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
    }

    private void ApplyWindowLayout(bool compactMode)
    {
        if (compactMode)
        {
            this.Size = new Vector2(560, 340);
            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(420, 220),
                MaximumSize = new Vector2(900, 620),
            };
            ImGui.SetWindowSize(new Vector2(560, 340), ImGuiCond.Appearing);
        }
        else
        {
            this.Size = new Vector2(1180, 520);
            this.SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(900, 320),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
            };
            ImGui.SetWindowSize(new Vector2(1180, 520), ImGuiCond.Appearing);
        }
    }

    private void DrawCompactToolbar()
    {
        ImGui.SetNextItemWidth(220);
        ImGui.InputTextWithHint("##compact-loot-filter", "Search loot...", ref this.filterText, 256);
        ImGui.SameLine();
        ImGui.TextUnformatted($"Entries: {this.lootCaptureService.Records.Count}");
        ImGui.SameLine();
        if (ImGui.Button("Clear"))
        {
            this.lootCaptureService.ClearHistory();
        }

        ImGui.SameLine();
        if (ImGui.Button("Settings"))
        {
            this.openConfigUi();
        }
    }

    private void DrawFullFilters(IReadOnlyList<string> categoryFilters, IReadOnlyList<string> zoneFilters)
    {
        ImGui.SetNextItemWidth(260);
        ImGui.InputTextWithHint("##loot-filter", "Search loot history...", ref this.filterText, 256);
        ImGui.SameLine();

        var showOnlySelfLoot = this.configuration.ShowOnlySelfLoot;
        if (ImGui.Checkbox("Self only", ref showOnlySelfLoot))
        {
            this.configuration.ShowOnlySelfLoot = showOnlySelfLoot;
            this.configuration.Save();
        }

        ImGui.SameLine();
        this.DrawCombo("##group-filter", GroupFilters, ref this.selectedGroupFilter, 120f);

        ImGui.SameLine();
        this.DrawCombo("##loot-type-filter", LootTypeFilters, ref this.selectedLootTypeFilter, 125f);

        ImGui.SameLine();
        this.DrawStringFilterCombo("##category-filter", "All categories", categoryFilters, ref this.selectedCategoryFilter, 150f);

        ImGui.SameLine();
        this.DrawStringFilterCombo("##zone-filter", "All zones", zoneFilters, ref this.selectedZoneFilter, 150f);

        ImGui.SameLine();
        if (ImGui.Button("Clear history"))
        {
            this.lootCaptureService.ClearHistory();
        }

        ImGui.SameLine();
        ImGui.TextUnformatted($"Entries: {this.lootCaptureService.Records.Count}");

        ImGui.SameLine();
        if (ImGui.Button("Settings"))
        {
            this.openConfigUi();
        }

        ImGui.SameLine();
        if (this.lootCaptureService.DebugModeEnabled && ImGui.Button("Open debug log"))
        {
            this.openDebugUi();
        }
    }

    private void DrawCompactTable(IReadOnlyList<LootRecord> records)
    {
        if (!ImGui.BeginTable("##compact-loot-history", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX, new Vector2(-1, -1)))
        {
            return;
        }

        ImGui.TableSetupColumn("Who", ImGuiTableColumnFlags.WidthFixed, 170f);
        ImGui.TableSetupColumn("Qty", ImGuiTableColumnFlags.WidthFixed, 70f);
        ImGui.TableSetupColumn("Loot", ImGuiTableColumnFlags.WidthStretch, 220f);
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

    private IReadOnlyList<TableColumnDefinition> BuildLootHistoryColumns()
    {
        var columns = new List<TableColumnDefinition>();
        var visibility = this.configuration.LootHistoryColumns;

        if (visibility.ShowTime)
        {
            columns.Add(new TableColumnDefinition("Time", ImGuiTableColumnFlags.WidthFixed, 150f, record => ImGui.TextUnformatted(record.CapturedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"))));
        }

        if (visibility.ShowZone)
        {
            columns.Add(new TableColumnDefinition("Zone", ImGuiTableColumnFlags.WidthFixed, 170f, record => this.DrawRecordTextCell(record, GetDisplayOrUnknown(record.ZoneName), "history-zone")));
        }

        if (visibility.ShowWho)
        {
            columns.Add(new TableColumnDefinition("Who", ImGuiTableColumnFlags.WidthFixed, 170f, record => this.DrawRecordTextCell(record, GetWhoLabel(record), "history-who", GetGroupColor(record.WhoConfidence))));
        }

        if (visibility.ShowGroup)
        {
            columns.Add(new TableColumnDefinition("Group", ImGuiTableColumnFlags.WidthFixed, 120f, record => this.DrawRecordTextCell(record, GetGroupLabel(record.WhoConfidence), "history-group", GetGroupColor(record.WhoConfidence))));
        }

        if (visibility.ShowQuantity)
        {
            columns.Add(new TableColumnDefinition("Qty", ImGuiTableColumnFlags.WidthFixed, 70f, record => this.DrawRecordTextCell(record, record.Quantity.ToString(), "history-quantity")));
        }

        if (visibility.ShowIcon && this.configuration.ShowItemIcons)
        {
            columns.Add(new TableColumnDefinition("Icon", ImGuiTableColumnFlags.WidthFixed, 42f, record => this.DrawIconCell(record, "history")));
        }

        if (visibility.ShowLoot)
        {
            columns.Add(new TableColumnDefinition("Loot", ImGuiTableColumnFlags.WidthStretch, 230f, record => this.DrawLootCell(record, "history")));
        }

        if (visibility.ShowRawLine)
        {
            columns.Add(new TableColumnDefinition("Raw Line", ImGuiTableColumnFlags.WidthStretch, 320f, record => this.DrawRecordTextCell(record, record.RawText, "history-raw")));
        }

        if (visibility.ShowCopy)
        {
            columns.Add(new TableColumnDefinition("Copy", ImGuiTableColumnFlags.WidthFixed, 72f, record => this.DrawCopyButton(record, "history")));
        }

        return columns;
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
            columns.Add(new TableColumnDefinition("Who", ImGuiTableColumnFlags.WidthFixed, 170f, record => this.DrawRecordTextCell(record, GetWhoLabel(record), "details-who", GetGroupColor(record.WhoConfidence))));
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

        ImGui.TextUnformatted("Overview reflects the current filters.");
        ImGui.Spacing();

        if (ImGui.BeginTable("##overview-summary-cards", 4, ImGuiTableFlags.SizingStretchSame))
        {
            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            this.DrawOverviewCard("Entries", summary.TotalEntries.ToString(), GetLootTypeFilterDescription());

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
        ImGui.TextUnformatted(title);
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

    private bool RecordMatchesCurrentFilters(LootRecord record)
    {
        if (this.filterText.Length != 0 && !RecordMatchesFilter(record, this.filterText))
        {
            return false;
        }

        if (this.configuration.UseCompactMainWindowByDefault)
        {
            return true;
        }

        if (this.configuration.ShowOnlySelfLoot && record.WhoConfidence != LootWhoConfidence.Self)
        {
            return false;
        }

        if (this.selectedLootTypeFilter != 0 && record.LootTypeBucket != GetSelectedLootTypeBucket(this.selectedLootTypeFilter))
        {
            return false;
        }

        if (this.selectedGroupFilter != 0 && GetGroupLabel(record.WhoConfidence) != GroupFilters[this.selectedGroupFilter])
        {
            return false;
        }

        var category = record.ItemCategoryLabel ?? "Unknown";
        if (!string.IsNullOrEmpty(this.selectedCategoryFilter) && !string.Equals(category, this.selectedCategoryFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var zone = string.IsNullOrWhiteSpace(record.ZoneName) ? "Unknown" : record.ZoneName;
        return string.IsNullOrEmpty(this.selectedZoneFilter)
            || string.Equals(zone, this.selectedZoneFilter, StringComparison.OrdinalIgnoreCase);
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
        if (ImGui.BeginPopupContextItem($"##record-context-{scope}-{record.CapturedAtUtc.ToUnixTimeMilliseconds()}-{record.RawText.GetHashCode()}"))
        {
            if (ImGui.MenuItem("Copy line"))
            {
                ImGui.SetClipboardText(this.BuildClipboardLine(record));
            }

            ImGui.Separator();

            if (record.ItemId is uint itemId)
            {
                if (ImGui.MenuItem($"Hide '{GetDisplayItemName(record)}'"))
                {
                    if (!this.configuration.BlacklistedItemIds.Contains(itemId))
                    {
                        this.configuration.BlacklistedItemIds.Add(itemId);
                        this.configuration.Save();
                    }
                }
            }
            else
            {
                ImGui.TextDisabled("Hide item unavailable");
                ImGui.TextDisabled("Item ID could not be resolved.");
            }

            ImGui.EndPopup();
        }
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
        if (ImGui.SmallButton($"Copy##{scope}-{record.CapturedAtUtc.ToUnixTimeMilliseconds()}-{record.RawText.GetHashCode()}"))
        {
            ImGui.SetClipboardText(this.BuildClipboardLine(record));
        }
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

    private void DrawCombo(string id, IReadOnlyList<string> options, ref int selectedIndex, float width)
    {
        ImGui.SetNextItemWidth(width);
        if (!ImGui.BeginCombo(id, options[selectedIndex]))
        {
            return;
        }

        for (var index = 0; index < options.Count; index++)
        {
            var selected = index == selectedIndex;
            if (ImGui.Selectable(options[index], selected))
            {
                selectedIndex = index;
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

    private static bool RecordMatchesFilter(LootRecord record, string filter)
    {
        return Contains(record.ZoneName, filter)
            || Contains(record.WhoName, filter)
            || Contains(record.WhoDisplayName, filter)
            || Contains(record.WhoWorldName, filter)
            || Contains(record.ItemName, filter)
            || Contains(GetLootTypeLabel(record.LootTypeBucket), filter)
            || Contains(record.ItemCategoryLabel, filter)
            || Contains(record.FilterGroupLabel, filter)
            || Contains(record.EquipSlotCategoryLabel, filter)
            || Contains(record.ResolvedItemName, filter)
            || Contains(record.Quantity.ToString(), filter)
            || Contains(FormatOptional(record.FilterGroupId), filter)
            || Contains(FormatOptional(record.EquipSlotCategoryId), filter)
            || Contains(FormatOptional(record.ItemUICategoryId), filter)
            || Contains(FormatOptional(record.ItemSearchCategoryId), filter)
            || Contains(FormatOptional(record.ItemSortCategoryId), filter)
            || Contains(FormatOptional(record.ItemId), filter)
            || Contains(record.RawText, filter);
    }

    private static bool Contains(string? value, string filter)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Contains(filter, StringComparison.OrdinalIgnoreCase);
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
        return record.ResolvedItemName ?? record.ItemName ?? record.RawText;
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

    private static LootTypeBucket GetSelectedLootTypeBucket(int selectedIndex)
    {
        return selectedIndex switch
        {
            1 => LootTypeBucket.Dungeon,
            2 => LootTypeBucket.Raid,
            _ => LootTypeBucket.Other,
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
        ImGui.BeginChild($"##overview-card-{title}", new Vector2(0, 78), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        ImGui.TextColored(new Vector4(0.40f, 0.80f, 1.00f, 1.0f), title);
        ImGui.Spacing();
        ImGui.TextUnformatted(value);
        ImGui.TextDisabled(subtitle);
        ImGui.EndChild();
    }

    private string GetLootTypeFilterDescription()
    {
        return this.selectedLootTypeFilter switch
        {
            1 => "Dungeon loot only",
            2 => "Raid loot only",
            _ => "All visible loot",
        };
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
            1 => new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
            2 => new Vector4(0.3f, 1.0f, 0.3f, 1.0f),
            3 => new Vector4(0.4f, 0.6f, 1.0f, 1.0f),
            4 => new Vector4(0.8f, 0.4f, 1.0f, 1.0f),
            7 => new Vector4(1.0f, 0.6f, 0.8f, 1.0f),
            _ => new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
        };
    }

    private static Vector4 GetGroupColor(LootWhoConfidence confidence)
    {
        return confidence switch
        {
            LootWhoConfidence.Self => new Vector4(0.45f, 0.95f, 0.45f, 1.0f),
            LootWhoConfidence.PartyOrAllianceVerified => new Vector4(0.45f, 0.75f, 1.0f, 1.0f),
            _ => new Vector4(0.90f, 0.90f, 0.90f, 1.0f),
        };
    }

    private sealed record TableColumnDefinition(string Label, ImGuiTableColumnFlags Flags, float Width, Action<LootRecord> DrawCell);
}
