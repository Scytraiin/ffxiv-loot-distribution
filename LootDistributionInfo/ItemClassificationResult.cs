namespace LootDistributionInfo;

public sealed class ItemClassificationResult
{
    public byte? FilterGroupId { get; init; }

    public string? FilterGroupLabel { get; init; }

    public uint? EquipSlotCategoryId { get; init; }

    public string? EquipSlotCategoryLabel { get; init; }

    public uint? ItemUICategoryId { get; init; }

    public uint? ItemSearchCategoryId { get; init; }

    public uint? ItemSortCategoryId { get; init; }

    public string? ResolvedItemName { get; init; }

    public string ClassificationSource { get; init; } = "Unresolved";

    public string ItemCategoryLabel =>
        ItemCategoryMappings.GetPrimaryCategoryLabel(this.FilterGroupId, this.FilterGroupLabel, this.EquipSlotCategoryLabel);

    public static ItemClassificationResult Unresolved() => new();
}
