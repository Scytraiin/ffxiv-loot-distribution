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

        var columnCount = this.lootCaptureService.DebugModeEnabled ? 7 : 5;
        if (!ImGui.BeginTable("##loot-history", columnCount, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(-1, -1)))
        {
            return;
        }

        ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 150f);
        ImGui.TableSetupColumn("Zone", ImGuiTableColumnFlags.WidthFixed, 180f);
        ImGui.TableSetupColumn("Who", ImGuiTableColumnFlags.WidthFixed, 170f);
        ImGui.TableSetupColumn("Loot", ImGuiTableColumnFlags.WidthStretch, 240f);
        ImGui.TableSetupColumn("Raw Line", ImGuiTableColumnFlags.WidthStretch, 320f);
        if (this.lootCaptureService.DebugModeEnabled)
        {
            ImGui.TableSetupColumn("Source", ImGuiTableColumnFlags.WidthFixed, 95f);
            ImGui.TableSetupColumn("Who Status", ImGuiTableColumnFlags.WidthFixed, 120f);
        }
        ImGui.TableHeadersRow();

        foreach (var record in visibleRecords)
        {
            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            ImGui.TextUnformatted(record.CapturedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));

            ImGui.TableSetColumnIndex(1);
            ImGui.TextWrapped(string.IsNullOrWhiteSpace(record.ZoneName) ? "Unknown" : record.ZoneName);

            ImGui.TableSetColumnIndex(2);
            ImGui.TextWrapped(record.WhoName ?? "Unknown");

            ImGui.TableSetColumnIndex(3);
            ImGui.TextWrapped(record.LootText ?? record.RawText);

            ImGui.TableSetColumnIndex(4);
            ImGui.TextWrapped(record.RawText);

            if (this.lootCaptureService.DebugModeEnabled)
            {
                ImGui.TableSetColumnIndex(5);
                ImGui.TextUnformatted(record.Source.ToString());

                ImGui.TableSetColumnIndex(6);
                ImGui.TextWrapped(GetWhoStatusLabel(record.WhoConfidence));
            }
        }

        ImGui.EndTable();
    }

    private static bool RecordMatchesFilter(LootRecord record, string filter)
    {
        return Contains(record.ZoneName, filter)
            || Contains(record.WhoName, filter)
            || Contains(record.LootText, filter)
            || Contains(record.RawText, filter);
    }

    private static bool Contains(string? value, string filter)
    {
        return !string.IsNullOrWhiteSpace(value) && value.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetWhoStatusLabel(LootWhoConfidence confidence)
    {
        return confidence switch
        {
            LootWhoConfidence.Self => "Self",
            LootWhoConfidence.PartyOrAllianceVerified => "Party/Alliance",
            LootWhoConfidence.TextOnly => "Text Only",
            _ => "Unknown",
        };
    }
}
