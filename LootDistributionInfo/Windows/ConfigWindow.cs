using System;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace LootDistributionInfo.Windows;

public sealed class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    private readonly LootCaptureService lootCaptureService;

    public ConfigWindow(Configuration configuration, LootCaptureService lootCaptureService)
        : base("Loot Distribution Info Settings")
    {
        this.configuration = configuration;
        this.lootCaptureService = lootCaptureService;
        this.Size = new Vector2(420, 160);
        this.SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        var retainHistory = this.configuration.RetainHistoryBetweenSessions;
        var maxEntries = this.configuration.MaxEntries;
        var changed = false;

        if (ImGui.Checkbox("Retain history between sessions", ref retainHistory))
        {
            this.configuration.RetainHistoryBetweenSessions = retainHistory;
            changed = true;
        }

        if (ImGui.DragInt("Max stored entries", ref maxEntries, 1f, 1, 5000))
        {
            this.configuration.MaxEntries = maxEntries;
            changed = true;
        }

        if (changed)
        {
            this.lootCaptureService.ApplyConfigurationChanges();
        }

        ImGui.Spacing();
        ImGui.TextWrapped("History is always kept in memory for the current session. Turning off retention only stops saving captured lines into the persisted plugin config.");
    }
}
