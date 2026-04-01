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
    public DateTimeOffset CapturedAtUtc { get; set; }

    public string ZoneName { get; set; } = string.Empty;

    public string RawText { get; set; } = string.Empty;

    public string? WhoName { get; set; }

    public string? WhoDisplayName { get; set; }

    public string? WhoWorldName { get; set; }

    public ushort? WhoHomeWorldId { get; set; }

    public int Quantity { get; set; } = 1;

    public string? ItemName { get; set; }

    public LootTypeBucket LootTypeBucket { get; set; }

    public List<LootRollEntry> RollEntries { get; set; } = [];

    public uint? ItemId { get; set; }

    public uint? IconId { get; set; }

    public uint? Rarity { get; set; }

    public bool IsHighQuality { get; set; }

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

    public LootWhoConfidence WhoConfidence { get; set; }

    [Obsolete("Legacy config migration only.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public string? PlayerName { get; set; }

    [Obsolete("Legacy config migration only.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public string? ItemText { get; set; }

    [Obsolete("Legacy config migration only.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public string? LootText { get; set; }

    public LootCaptureSource Source { get; set; }

    public void Normalize()
    {
#pragma warning disable CS0618
        this.ZoneName = this.ZoneName?.Trim() ?? string.Empty;
        this.RawText = this.RawText?.Trim() ?? string.Empty;
        this.WhoName = NormalizeNullable(this.WhoName);
        this.WhoDisplayName = NormalizeNullable(this.WhoDisplayName) ?? this.WhoName;
        this.WhoWorldName = NormalizeNullable(this.WhoWorldName);
        this.Quantity = this.Quantity <= 0 ? 1 : this.Quantity;
        this.ItemName = NormalizeNullable(this.ItemName);
        this.RollEntries ??= [];
        foreach (var entry in this.RollEntries)
        {
            entry.Normalize();
        }
        var needsLegacyLootMigration = this.ItemName is null;
        this.ItemCategoryLabel = NormalizeNullable(this.ItemCategoryLabel);
        this.FilterGroupLabel = NormalizeNullable(this.FilterGroupLabel);
        this.EquipSlotCategoryLabel = NormalizeNullable(this.EquipSlotCategoryLabel);
        this.ResolvedItemName = NormalizeNullable(this.ResolvedItemName);
        this.ClassificationSource = NormalizeNullable(this.ClassificationSource);

        if (this.WhoName is null && !string.IsNullOrWhiteSpace(this.PlayerName))
        {
            this.WhoName = NormalizeNullable(this.PlayerName);
            this.WhoDisplayName ??= this.WhoName;
        }

        var legacyLootText = this.ItemName ?? NormalizeNullable(this.LootText) ?? NormalizeNullable(this.ItemText);
        if (!string.IsNullOrWhiteSpace(legacyLootText))
        {
            var (quantity, itemName) = LootQuantityParser.Split(legacyLootText);
            if (needsLegacyLootMigration)
            {
                this.Quantity = quantity;
                this.ItemName = itemName;
            }
            else
            {
                this.Quantity = this.Quantity <= 0 ? quantity : this.Quantity;
                this.ItemName ??= itemName;
            }
        }
#pragma warning restore CS0618
    }

    private static string? NormalizeNullable(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
