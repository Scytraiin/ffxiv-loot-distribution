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

    private readonly LootCaptureService lootCaptureService;
    private readonly Configuration configuration;
    private readonly ITextureProvider textureProvider;
    private readonly Action openConfigUi;
    private readonly Action openDebugUi;
    private string filterText = string.Empty;
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

        this.Size = new Vector2(1180, 520);
        this.SizeCondition = ImGuiCond.FirstUseEver;
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(900, 320),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        ImGui.TextWrapped("Tracks loot messages and keeps a searchable history of where the loot happened and who received it.");
        ImGui.Separator();

        var nonBlacklistedRecords = this.lootCaptureService.Records
            .Where(record => !this.IsBlacklisted(record))
            .ToList();

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

        this.DrawFilters(categoryFilters, zoneFilters);

        ImGui.Spacing();

        var visibleRecords = nonBlacklistedRecords
            .Where(this.RecordMatchesCurrentFilters)
            .ToList();

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
            this.DrawLootHistoryTable(visibleRecords);
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Item Details"))
        {
            this.DrawItemDetailsTable(visibleRecords);
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Overview"))
        {
            this.DrawOverviewTab(visibleRecords);
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }

    private void DrawFilters(IReadOnlyList<string> categoryFilters, IReadOnlyList<string> zoneFilters)
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

    private void DrawLootHistoryTable(IReadOnlyList<LootRecord> records)
    {
        var columnCount = this.lootCaptureService.DebugModeEnabled ? 10 : 9;
        if (!ImGui.BeginTable("##loot-history", columnCount, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX, new Vector2(-1, -1)))
        {
            return;
        }

        ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 150f);
        ImGui.TableSetupColumn("Zone", ImGuiTableColumnFlags.WidthFixed, 170f);
        ImGui.TableSetupColumn("Who", ImGuiTableColumnFlags.WidthFixed, 160f);
        ImGui.TableSetupColumn("Group", ImGuiTableColumnFlags.WidthFixed, 120f);
        ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthFixed, 42f);
        ImGui.TableSetupColumn("Loot", ImGuiTableColumnFlags.WidthStretch, 230f);
        ImGui.TableSetupColumn("Rolls", ImGuiTableColumnFlags.WidthStretch, 220f);
        ImGui.TableSetupColumn("Raw Line", ImGuiTableColumnFlags.WidthStretch, 320f);
        ImGui.TableSetupColumn("Copy", ImGuiTableColumnFlags.WidthFixed, 72f);
        if (this.lootCaptureService.DebugModeEnabled)
        {
            ImGui.TableSetupColumn("Source", ImGuiTableColumnFlags.WidthFixed, 95f);
        }

        ImGui.TableHeadersRow();

        foreach (var record in records)
        {
            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            ImGui.TextUnformatted(record.CapturedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));

            ImGui.TableSetColumnIndex(1);
            this.DrawTruncatedCellText(string.IsNullOrWhiteSpace(record.ZoneName) ? "Unknown" : record.ZoneName);

            ImGui.TableSetColumnIndex(2);
            this.DrawTruncatedCellText(record.WhoName ?? "Unknown");

            ImGui.TableSetColumnIndex(3);
            this.DrawTruncatedCellText(GetGroupLabel(record.WhoConfidence));

            ImGui.TableSetColumnIndex(4);
            this.DrawIconCell(record, "history");

            ImGui.TableSetColumnIndex(5);
            this.DrawLootCell(record, "history");

            ImGui.TableSetColumnIndex(6);
            this.DrawTruncatedCellText(record.RollsText);

            ImGui.TableSetColumnIndex(7);
            this.DrawTruncatedCellText(record.RawText);

            ImGui.TableSetColumnIndex(8);
            this.DrawCopyButton(record, "history");

            if (this.lootCaptureService.DebugModeEnabled)
            {
                ImGui.TableSetColumnIndex(9);
                ImGui.TextUnformatted(record.Source.ToString());
            }
        }

        ImGui.EndTable();
    }

    private void DrawItemDetailsTable(IReadOnlyList<LootRecord> records)
    {
        var columnCount = this.lootCaptureService.DebugModeEnabled ? 16 : 15;
        if (!ImGui.BeginTable("##loot-item-details", columnCount, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX, new Vector2(-1, -1)))
        {
            return;
        }

        ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 150f);
        ImGui.TableSetupColumn("Zone", ImGuiTableColumnFlags.WidthFixed, 170f);
        ImGui.TableSetupColumn("Who", ImGuiTableColumnFlags.WidthFixed, 160f);
        ImGui.TableSetupColumn("Group", ImGuiTableColumnFlags.WidthFixed, 120f);
        ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthFixed, 42f);
        ImGui.TableSetupColumn("Loot", ImGuiTableColumnFlags.WidthStretch, 220f);
        ImGui.TableSetupColumn("Category", ImGuiTableColumnFlags.WidthFixed, 150f);
        ImGui.TableSetupColumn("Filter Group", ImGuiTableColumnFlags.WidthFixed, 170f);
        ImGui.TableSetupColumn("Equip Slot", ImGuiTableColumnFlags.WidthFixed, 170f);
        ImGui.TableSetupColumn("UI Category", ImGuiTableColumnFlags.WidthFixed, 100f);
        ImGui.TableSetupColumn("Search Category", ImGuiTableColumnFlags.WidthFixed, 120f);
        ImGui.TableSetupColumn("Sort Category", ImGuiTableColumnFlags.WidthFixed, 100f);
        ImGui.TableSetupColumn("Rolls", ImGuiTableColumnFlags.WidthStretch, 220f);
        ImGui.TableSetupColumn("Raw Line", ImGuiTableColumnFlags.WidthStretch, 320f);
        ImGui.TableSetupColumn("Copy", ImGuiTableColumnFlags.WidthFixed, 72f);
        if (this.lootCaptureService.DebugModeEnabled)
        {
            ImGui.TableSetupColumn("Source", ImGuiTableColumnFlags.WidthFixed, 95f);
        }

        ImGui.TableHeadersRow();

        foreach (var record in records)
        {
            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            ImGui.TextUnformatted(record.CapturedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));

            ImGui.TableSetColumnIndex(1);
            this.DrawTruncatedCellText(string.IsNullOrWhiteSpace(record.ZoneName) ? "Unknown" : record.ZoneName);

            ImGui.TableSetColumnIndex(2);
            this.DrawTruncatedCellText(record.WhoName ?? "Unknown");

            ImGui.TableSetColumnIndex(3);
            this.DrawTruncatedCellText(GetGroupLabel(record.WhoConfidence));

            ImGui.TableSetColumnIndex(4);
            this.DrawIconCell(record, "details");

            ImGui.TableSetColumnIndex(5);
            this.DrawLootCell(record, "details");

            ImGui.TableSetColumnIndex(6);
            this.DrawTruncatedCellText(record.ItemCategoryLabel ?? "Unknown");

            ImGui.TableSetColumnIndex(7);
            this.DrawTruncatedCellText(FormatLabelWithId(record.FilterGroupLabel, record.FilterGroupId));

            ImGui.TableSetColumnIndex(8);
            this.DrawTruncatedCellText(FormatLabelWithId(record.EquipSlotCategoryLabel, record.EquipSlotCategoryId));

            ImGui.TableSetColumnIndex(9);
            this.DrawTruncatedCellText(FormatOptional(record.ItemUICategoryId));

            ImGui.TableSetColumnIndex(10);
            this.DrawTruncatedCellText(FormatOptional(record.ItemSearchCategoryId));

            ImGui.TableSetColumnIndex(11);
            this.DrawTruncatedCellText(FormatOptional(record.ItemSortCategoryId));

            ImGui.TableSetColumnIndex(12);
            this.DrawTruncatedCellText(record.RollsText);

            ImGui.TableSetColumnIndex(13);
            this.DrawTruncatedCellText(record.RawText);

            ImGui.TableSetColumnIndex(14);
            this.DrawCopyButton(record, "details");

            if (this.lootCaptureService.DebugModeEnabled)
            {
                ImGui.TableSetColumnIndex(15);
                ImGui.TextUnformatted(record.Source.ToString());
            }
        }

        ImGui.EndTable();
    }

    private void DrawOverviewTab(IReadOnlyList<LootRecord> records)
    {
        var summary = LootOverviewSummary.Build(records);

        ImGui.TextUnformatted("Overview reflects the current filters.");
        ImGui.Spacing();

        if (ImGui.BeginTable("##overview-summary", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("Total");
            ImGui.TableSetupColumn("Unique");
            ImGui.TableSetupColumn("Latest");
            ImGui.TableSetupColumn("Visible Categories");
            ImGui.TableHeadersRow();

            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            ImGui.TextUnformatted(summary.TotalEntries.ToString());

            ImGui.TableSetColumnIndex(1);
            ImGui.TextUnformatted(summary.UniqueItems.ToString());

            ImGui.TableSetColumnIndex(2);
            ImGui.TextUnformatted(summary.LatestItemAtUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "-");

            ImGui.TableSetColumnIndex(3);
            ImGui.TextUnformatted(summary.TopCategories.Count.ToString());

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
            }

            if (showIcons)
            {
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

        if (this.configuration.ShowOnlySelfLoot && record.WhoConfidence != LootWhoConfidence.Self)
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
        var label = record.ResolvedItemName ?? record.LootText ?? record.RawText;
        if (record.IsHighQuality)
        {
            label = $"{label} HQ";
        }

        var truncated = this.DrawColoredItemText(record, label);
        this.DrawTooltipAndContext(record, $"{scope}-loot", truncated);
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
                    this.DrawTextTooltip(record.ResolvedItemName ?? record.LootText ?? record.RawText);
                }
            }
            else if (this.configuration.ShowItemTooltips && scope.Contains("-icon", StringComparison.Ordinal))
            {
                this.DrawItemTooltip(record);
            }
        }

        if (ImGui.BeginPopupContextItem($"##record-context-{scope}-{record.CapturedAtUtc.ToUnixTimeMilliseconds()}-{record.RawText.GetHashCode()}"))
        {
            if (ImGui.MenuItem("Copy line"))
            {
                ImGui.SetClipboardText(this.BuildClipboardLine(record));
            }

            ImGui.Separator();

            if (record.ItemId is uint itemId)
            {
                if (ImGui.MenuItem($"Hide '{record.ResolvedItemName ?? record.LootText ?? "item"}'"))
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
        ImGui.TextUnformatted(record.ResolvedItemName ?? record.LootText ?? record.RawText);

        if (record.IsHighQuality)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(1.0f, 0.86f, 0.3f, 1.0f), "HQ");
        }

        ImGui.Separator();
        ImGui.TextUnformatted($"When: {record.CapturedAtUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss}");
        ImGui.TextUnformatted($"Zone: {GetDisplayOrUnknown(record.ZoneName)}");
        ImGui.TextUnformatted($"Group: {GetGroupLabel(record.WhoConfidence)}");
        ImGui.TextUnformatted($"Category: {record.ItemCategoryLabel ?? "Unknown"}");
        ImGui.TextUnformatted($"Filter Group: {FormatLabelWithId(record.FilterGroupLabel, record.FilterGroupId)}");
        ImGui.TextUnformatted($"Equip Slot: {FormatLabelWithId(record.EquipSlotCategoryLabel, record.EquipSlotCategoryId)}");
        ImGui.TextUnformatted($"Rolls: {record.RollsText}");
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

    private void DrawTruncatedCellText(string text)
    {
        var displayText = GetTruncatedDisplayText(text, this.GetAvailableCellTextWidth(), out var truncated);
        ImGui.TextUnformatted(displayText);

        if (truncated && ImGui.IsItemHovered())
        {
            this.DrawTextTooltip(text);
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
            || Contains(record.LootText, filter)
            || Contains(record.ItemCategoryLabel, filter)
            || Contains(record.FilterGroupLabel, filter)
            || Contains(record.EquipSlotCategoryLabel, filter)
            || Contains(record.ResolvedItemName, filter)
            || Contains(record.RollsText, filter)
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
        var who = record.WhoName ?? "Unknown";
        var loot = record.ResolvedItemName ?? record.LootText ?? record.RawText;
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
}
