using System;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace LootDistributionInfo.Windows;

public sealed class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    private readonly LootCaptureService lootCaptureService;
    private readonly Action openDebugUi;

    public ConfigWindow(Configuration configuration, LootCaptureService lootCaptureService, Action openDebugUi)
        : base("Loot History Settings")
    {
        this.configuration = configuration;
        this.lootCaptureService = lootCaptureService;
        this.openDebugUi = openDebugUi;
        this.Size = new Vector2(460, 220);
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
        var changed = false;
        var debugModeWasEnabled = this.configuration.DebugModeEnabled;

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

        if (changed)
        {
            this.lootCaptureService.ApplyConfigurationChanges();

            if (!debugModeWasEnabled && this.configuration.DebugModeEnabled)
            {
                this.openDebugUi();
            }
        }

        ImGui.Spacing();
        ImGui.TextWrapped("Your loot history can stay available between sessions. Turn this off if you only want to keep the current play session.");

        if (this.configuration.DebugModeEnabled)
        {
            ImGui.Spacing();
            if (ImGui.Button("Open debug log"))
            {
                this.openDebugUi();
            }
        }
    }
}
