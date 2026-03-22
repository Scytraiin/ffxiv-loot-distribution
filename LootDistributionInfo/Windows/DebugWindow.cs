using System;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace LootDistributionInfo.Windows;

public sealed class DebugWindow : Window, IDisposable
{
    private readonly LootCaptureService lootCaptureService;

    public DebugWindow(LootCaptureService lootCaptureService)
        : base("Loot Debug Log")
    {
        this.lootCaptureService = lootCaptureService;
        this.Size = new Vector2(860, 320);
        this.SizeCondition = ImGuiCond.FirstUseEver;
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(640, 220),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        if (!this.lootCaptureService.DebugModeEnabled)
        {
            ImGui.TextWrapped("Enable debug tools in settings to see the live capture log.");
            return;
        }

        if (ImGui.Button("Clear debug log"))
        {
            this.lootCaptureService.ClearDebugEvents();
        }

        ImGui.SameLine();
        ImGui.TextUnformatted($"Events: {this.lootCaptureService.DebugEvents.Count}");
        ImGui.Separator();

        if (this.lootCaptureService.DebugEvents.Count == 0)
        {
            ImGui.TextWrapped("Debug logging is idle. New capture and parser events will appear here while debug tools are enabled.");
            return;
        }

        if (!ImGui.BeginTable("##loot-debug-log", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new Vector2(-1, -1)))
        {
            return;
        }

        ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 150f);
        ImGui.TableSetupColumn("Area", ImGuiTableColumnFlags.WidthFixed, 180f);
        ImGui.TableSetupColumn("Event", ImGuiTableColumnFlags.WidthFixed, 120f);
        ImGui.TableSetupColumn("Details", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        foreach (var record in this.lootCaptureService.DebugEvents)
        {
            ImGui.TableNextRow();

            ImGui.TableSetColumnIndex(0);
            ImGui.TextUnformatted(record.CapturedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));

            ImGui.TableSetColumnIndex(1);
            ImGui.TextWrapped(record.Area);

            ImGui.TableSetColumnIndex(2);
            ImGui.TextWrapped(record.Event);

            ImGui.TableSetColumnIndex(3);
            ImGui.TextWrapped(record.Details);
        }

        ImGui.EndTable();
    }
}
