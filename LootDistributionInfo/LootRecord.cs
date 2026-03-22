using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace LootDistributionInfo;

[Serializable]
public enum LootWhoConfidence
{
    Unknown = 0,
    Self = 1,
    PartyOrAllianceVerified = 2,
    TextOnly = 3,
}

[Serializable]
public sealed class LootRecord
{
    public const string NoRollsLabel = "No rolls";

    public DateTimeOffset CapturedAtUtc { get; set; }

    public string ZoneName { get; set; } = string.Empty;

    public string RawText { get; set; } = string.Empty;

    public string? WhoName { get; set; }

    public string? LootText { get; set; }

    public string? ItemCategoryLabel { get; set; }

    public byte? FilterGroupId { get; set; }

    public string? FilterGroupLabel { get; set; }

    public uint? EquipSlotCategoryId { get; set; }

    public string? EquipSlotCategoryLabel { get; set; }

    public uint? ItemUICategoryId { get; set; }

    public uint? ItemSearchCategoryId { get; set; }

    public uint? ItemSortCategoryId { get; set; }

    public string? ResolvedItemName { get; set; }

    public string? ClassificationSource { get; set; }

    public string RollsText { get; set; } = NoRollsLabel;

    public List<LootRollRecord> RollEntries { get; set; } = [];

    public LootWhoConfidence WhoConfidence { get; set; }

    [Obsolete("Legacy config migration only.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public string? PlayerName { get; set; }

    [Obsolete("Legacy config migration only.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public string? ItemText { get; set; }

    public LootCaptureSource Source { get; set; }

    public void Normalize()
    {
#pragma warning disable CS0618
        this.ZoneName = this.ZoneName?.Trim() ?? string.Empty;
        this.RawText = this.RawText?.Trim() ?? string.Empty;
        this.WhoName = NormalizeNullable(this.WhoName);
        this.LootText = NormalizeNullable(this.LootText);
        this.ItemCategoryLabel = NormalizeNullable(this.ItemCategoryLabel);
        this.FilterGroupLabel = NormalizeNullable(this.FilterGroupLabel);
        this.EquipSlotCategoryLabel = NormalizeNullable(this.EquipSlotCategoryLabel);
        this.ResolvedItemName = NormalizeNullable(this.ResolvedItemName);
        this.ClassificationSource = NormalizeNullable(this.ClassificationSource);
        this.RollEntries ??= [];

        if (this.WhoName is null && !string.IsNullOrWhiteSpace(this.PlayerName))
        {
            this.WhoName = NormalizeNullable(this.PlayerName);
        }

        if (this.LootText is null && !string.IsNullOrWhiteSpace(this.ItemText))
        {
            this.LootText = NormalizeNullable(this.ItemText);
        }

        foreach (var rollEntry in this.RollEntries)
        {
            rollEntry.Normalize();
        }

        this.RollsText = this.RollEntries.Count == 0
            ? NoRollsLabel
            : string.Join("; ", this.RollEntries.Select(entry => entry.ToSummaryText()));
#pragma warning restore CS0618
    }

    public void AttachRolls(IEnumerable<LootRollRecord> rollEntries)
    {
        this.RollEntries = [.. rollEntries];
        this.Normalize();
    }

    private static string? NormalizeNullable(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
