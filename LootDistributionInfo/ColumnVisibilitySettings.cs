using System;

namespace LootDistributionInfo;

[Serializable]
public sealed class LootHistoryColumnVisibility
{
    public bool ShowTime { get; set; } = true;
    public bool ShowZone { get; set; } = true;
    public bool ShowWho { get; set; } = true;
    public bool ShowGroup { get; set; } = true;
    public bool ShowQuantity { get; set; } = true;
    public bool ShowIcon { get; set; } = true;
    public bool ShowLoot { get; set; } = true;
    public bool ShowRawLine { get; set; } = true;
    public bool ShowCopy { get; set; } = true;
}

[Serializable]
public sealed class ItemDetailsColumnVisibility
{
    public bool ShowTime { get; set; } = true;
    public bool ShowZone { get; set; } = true;
    public bool ShowWho { get; set; } = true;
    public bool ShowGroup { get; set; } = true;
    public bool ShowQuantity { get; set; } = true;
    public bool ShowIcon { get; set; } = true;
    public bool ShowLoot { get; set; } = true;
    public bool ShowCategory { get; set; } = true;
    public bool ShowFilterGroup { get; set; } = true;
    public bool ShowEquipSlot { get; set; } = true;
    public bool ShowUiCategory { get; set; } = true;
    public bool ShowSearchCategory { get; set; } = true;
    public bool ShowSortCategory { get; set; } = true;
    public bool ShowRawLine { get; set; } = true;
    public bool ShowCopy { get; set; } = true;
}
