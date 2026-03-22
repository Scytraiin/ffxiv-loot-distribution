using System;
using System.Linq;
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
        this.Size = new Vector2(560, 420);
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
        var showOnlySelfLoot = this.configuration.ShowOnlySelfLoot;
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

        if (ImGui.Checkbox("Default to self-only filter", ref showOnlySelfLoot))
        {
            this.configuration.ShowOnlySelfLoot = showOnlySelfLoot;
            changed = true;
        }

        if (changed)
        {
            this.lootCaptureService.ApplyConfigurationChanges();
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
                    ?? this.lootCaptureService.Records.FirstOrDefault(record => record.ItemId == itemId)?.LootText
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
}
