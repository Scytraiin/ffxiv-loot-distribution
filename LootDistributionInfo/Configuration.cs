using System;
using System.Collections.Generic;

using Dalamud.Configuration;
using Dalamud.Plugin;

namespace LootDistributionInfo;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public const int DefaultMaxEntries = 500;
    public const int CurrentVersion = 5;

    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public int Version { get; set; } = CurrentVersion;

    public bool RetainHistoryBetweenSessions { get; set; } = true;

    public int MaxEntries { get; set; } = DefaultMaxEntries;

    public bool DebugModeEnabled { get; set; }

    public List<LootRecord> StoredRecords { get; set; } = [];

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
        this.MigrateFromLegacyRecords();
        this.Normalize();
    }

    public void Save()
    {
        this.pluginInterface?.SavePluginConfig(this);
    }

    public void Normalize()
    {
        this.Version = CurrentVersion;
        this.MaxEntries = Math.Clamp(this.MaxEntries, 1, 5000);
        this.StoredRecords ??= [];

        foreach (var record in this.StoredRecords)
        {
            record.Normalize();
        }

        if (this.StoredRecords.Count > this.MaxEntries)
        {
            this.StoredRecords.RemoveRange(this.MaxEntries, this.StoredRecords.Count - this.MaxEntries);
        }
    }

    public void MigrateFromLegacyRecords()
    {
#pragma warning disable CS0618
        foreach (var record in this.StoredRecords)
        {
            if (!string.IsNullOrWhiteSpace(record.PlayerName) && string.IsNullOrWhiteSpace(record.WhoName))
            {
                record.WhoName = NormalizeNullable(record.PlayerName);
            }

            if (!string.IsNullOrWhiteSpace(record.ItemText) && string.IsNullOrWhiteSpace(record.LootText))
            {
                record.LootText = NormalizeNullable(record.ItemText);
            }

            record.PlayerName = null;
            record.ItemText = null;

            record.Normalize();
        }
#pragma warning restore CS0618
    }

    private static string? NormalizeNullable(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
