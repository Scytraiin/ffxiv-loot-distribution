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
    private string filterText = string.Empty;

    public MainWindow(LootCaptureService lootCaptureService, Action openConfigUi)
        : base("Loot Distribution Info")
    {
        this.lootCaptureService = lootCaptureService;
        this.openConfigUi = openConfigUi;

        this.Size = new Vector2(760, 420);
        this.SizeCondition = ImGuiCond.FirstUseEver;
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(560, 260),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        ImGui.TextWrapped("Tracks chat and log lines that look like loot acquisition messages. This first version keeps the matcher broad on purpose so it catches common obtain/obtained/obtains lines.");
        ImGui.Separator();

        ImGui.SetNextItemWidth(280);
        ImGui.InputTextWithHint("##loot-filter", "Filter captured lines...", ref this.filterText, 256);
        ImGui.SameLine();
        if (ImGui.Button("Clear history"))
        {
            this.lootCaptureService.ClearHistory();
        }
        ImGui.SameLine();
        ImGui.TextUnformatted($"Captured: {this.lootCaptureService.Records.Count}");
        ImGui.SameLine();
        if (ImGui.Button("Open settings"))
        {
            this.openConfigUi();
        }
        ImGui.SameLine();
        ImGui.TextUnformatted("Commands: /lootinfo, /lootinfo config");

        ImGui.Spacing();

        var visibleRecords = this.lootCaptureService.Records
            .Where(record => this.filterText.Length == 0 || record.RawText.Contains(this.filterText, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (visibleRecords.Count == 0)
        {
            ImGui.TextUnformatted("No loot lines captured yet.");
            return;
        }

        if (!ImGui.BeginTable("##loot-history", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(-1, -1)))
        {
            return;
        }

        ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 150f);
        ImGui.TableSetupColumn("Source", ImGuiTableColumnFlags.WidthFixed, 100f);
        ImGui.TableSetupColumn("Raw Text", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        foreach (var record in visibleRecords)
        {
            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            ImGui.TextUnformatted(record.CapturedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));

            ImGui.TableSetColumnIndex(1);
            ImGui.TextUnformatted(record.Source.ToString());

            ImGui.TableSetColumnIndex(2);
            ImGui.TextWrapped(record.RawText);
        }

        ImGui.EndTable();
    }
}
