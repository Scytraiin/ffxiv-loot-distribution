using System;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace LootDistributionInfo.Windows;

public sealed class MainWindow : Window, IDisposable
{
    private readonly LootCaptureService lootCaptureService;
    private readonly Action openConfigUi;
    private readonly Action openDebugUi;
    private string filterText = string.Empty;

    public MainWindow(LootCaptureService lootCaptureService, Action openConfigUi, Action openDebugUi)
        : base("Loot History")
    {
        this.lootCaptureService = lootCaptureService;
        this.openConfigUi = openConfigUi;
        this.openDebugUi = openDebugUi;

        this.Size = new Vector2(980, 440);
        this.SizeCondition = ImGuiCond.FirstUseEver;
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(760, 280),
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

        ImGui.SetNextItemWidth(280);
        ImGui.InputTextWithHint("##loot-filter", "Search loot history...", ref this.filterText, 256);
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

        ImGui.Spacing();

        var visibleRecords = this.lootCaptureService.Records
            .Where(record => this.filterText.Length == 0 || RecordMatchesFilter(record, this.filterText))
            .ToList();

        if (visibleRecords.Count == 0)
        {
            ImGui.TextUnformatted("No loot recorded yet.");
            return;
        }

        if (!ImGui.BeginTabBar("##loot-history-tabs"))
        {
            return;
        }

        if (ImGui.BeginTabItem("Loot History"))
        {
            DrawLootHistoryTable(visibleRecords, this.lootCaptureService.DebugModeEnabled);
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Item Details"))
        {
            DrawItemDetailsTable(visibleRecords, this.lootCaptureService.DebugModeEnabled);
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
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

    private static void DrawLootHistoryTable(System.Collections.Generic.IReadOnlyList<LootRecord> records, bool debugModeEnabled)
    {
        var columnCount = debugModeEnabled ? 8 : 7;
        if (!ImGui.BeginTable("##loot-history", columnCount, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX, new Vector2(-1, -1)))
        {
            return;
        }

        ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 150f);
        ImGui.TableSetupColumn("Zone", ImGuiTableColumnFlags.WidthFixed, 180f);
        ImGui.TableSetupColumn("Who", ImGuiTableColumnFlags.WidthFixed, 170f);
        ImGui.TableSetupColumn("Group", ImGuiTableColumnFlags.WidthFixed, 120f);
        ImGui.TableSetupColumn("Loot", ImGuiTableColumnFlags.WidthStretch, 240f);
        ImGui.TableSetupColumn("Rolls", ImGuiTableColumnFlags.WidthStretch, 220f);
        ImGui.TableSetupColumn("Raw Line", ImGuiTableColumnFlags.WidthStretch, 320f);
        if (debugModeEnabled)
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
            ImGui.TextWrapped(string.IsNullOrWhiteSpace(record.ZoneName) ? "Unknown" : record.ZoneName);

            ImGui.TableSetColumnIndex(2);
            ImGui.TextWrapped(record.WhoName ?? "Unknown");

            ImGui.TableSetColumnIndex(3);
            ImGui.TextWrapped(GetGroupLabel(record.WhoConfidence));

            ImGui.TableSetColumnIndex(4);
            ImGui.TextWrapped(record.LootText ?? record.RawText);

            ImGui.TableSetColumnIndex(5);
            ImGui.TextWrapped(record.RollsText);

            ImGui.TableSetColumnIndex(6);
            ImGui.TextWrapped(record.RawText);

            if (debugModeEnabled)
            {
                ImGui.TableSetColumnIndex(7);
                ImGui.TextUnformatted(record.Source.ToString());
            }
        }

        ImGui.EndTable();
    }

    private static void DrawItemDetailsTable(System.Collections.Generic.IReadOnlyList<LootRecord> records, bool debugModeEnabled)
    {
        var columnCount = debugModeEnabled ? 14 : 13;
        if (!ImGui.BeginTable("##loot-item-details", columnCount, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.ScrollX, new Vector2(-1, -1)))
        {
            return;
        }

        ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 150f);
        ImGui.TableSetupColumn("Zone", ImGuiTableColumnFlags.WidthFixed, 180f);
        ImGui.TableSetupColumn("Who", ImGuiTableColumnFlags.WidthFixed, 170f);
        ImGui.TableSetupColumn("Group", ImGuiTableColumnFlags.WidthFixed, 120f);
        ImGui.TableSetupColumn("Loot", ImGuiTableColumnFlags.WidthStretch, 220f);
        ImGui.TableSetupColumn("Category", ImGuiTableColumnFlags.WidthFixed, 150f);
        ImGui.TableSetupColumn("Filter Group", ImGuiTableColumnFlags.WidthFixed, 170f);
        ImGui.TableSetupColumn("Equip Slot", ImGuiTableColumnFlags.WidthFixed, 170f);
        ImGui.TableSetupColumn("UI Category", ImGuiTableColumnFlags.WidthFixed, 100f);
        ImGui.TableSetupColumn("Search Category", ImGuiTableColumnFlags.WidthFixed, 120f);
        ImGui.TableSetupColumn("Sort Category", ImGuiTableColumnFlags.WidthFixed, 100f);
        ImGui.TableSetupColumn("Rolls", ImGuiTableColumnFlags.WidthStretch, 220f);
        ImGui.TableSetupColumn("Raw Line", ImGuiTableColumnFlags.WidthStretch, 320f);
        if (debugModeEnabled)
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
            ImGui.TextWrapped(string.IsNullOrWhiteSpace(record.ZoneName) ? "Unknown" : record.ZoneName);

            ImGui.TableSetColumnIndex(2);
            ImGui.TextWrapped(record.WhoName ?? "Unknown");

            ImGui.TableSetColumnIndex(3);
            ImGui.TextWrapped(GetGroupLabel(record.WhoConfidence));

            ImGui.TableSetColumnIndex(4);
            ImGui.TextWrapped(record.LootText ?? record.RawText);

            ImGui.TableSetColumnIndex(5);
            ImGui.TextWrapped(record.ItemCategoryLabel ?? "Unknown");

            ImGui.TableSetColumnIndex(6);
            ImGui.TextWrapped(FormatLabelWithId(record.FilterGroupLabel, record.FilterGroupId));

            ImGui.TableSetColumnIndex(7);
            ImGui.TextWrapped(FormatLabelWithId(record.EquipSlotCategoryLabel, record.EquipSlotCategoryId));

            ImGui.TableSetColumnIndex(8);
            ImGui.TextUnformatted(FormatOptional(record.ItemUICategoryId));

            ImGui.TableSetColumnIndex(9);
            ImGui.TextUnformatted(FormatOptional(record.ItemSearchCategoryId));

            ImGui.TableSetColumnIndex(10);
            ImGui.TextUnformatted(FormatOptional(record.ItemSortCategoryId));

            ImGui.TableSetColumnIndex(11);
            ImGui.TextWrapped(record.RollsText);

            ImGui.TableSetColumnIndex(12);
            ImGui.TextWrapped(record.RawText);

            if (debugModeEnabled)
            {
                ImGui.TableSetColumnIndex(13);
                ImGui.TextUnformatted(record.Source.ToString());
            }
        }

        ImGui.EndTable();
    }

    private static string FormatLabelWithId(string? label, object? id)
    {
        var formattedId = FormatOptional(id);
        if (string.IsNullOrWhiteSpace(label))
        {
            return formattedId;
        }

        return formattedId.Length == 0 ? label : $"{label} ({formattedId})";
    }

    private static string FormatOptional(object? value)
    {
        return value?.ToString() ?? string.Empty;
    }
}
