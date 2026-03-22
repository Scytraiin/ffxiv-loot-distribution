using System.Collections.Generic;

namespace LootDistributionInfo;

public static class ItemCategoryMappings
{
    private static readonly IReadOnlyDictionary<byte, string> FilterGroupLabels = new Dictionary<byte, string>
    {
        [1] = "Physical Weapon",
        [2] = "Magical Weapon",
        [3] = "Shield",
        [4] = "Gear",
        [5] = "Meal",
        [6] = "Medicine",
        [7] = "Deep Dungeon Usable",
        [8] = "Potion",
        [9] = "Ether",
        [10] = "Elixir",
        [11] = "Crystal",
        [12] = "Crafting Material",
        [13] = "Materia",
        [14] = "Housing",
        [15] = "Stain",
        [16] = "Misc",
        [17] = "Fishing Bait",
        [18] = "Treasure Map",
        [19] = "Usable",
        [20] = "Gardening Seed",
        [21] = "Gardening Soil",
        [22] = "Gardening Fertilizer",
        [23] = "Secret Recipe Book",
        [24] = "Unused",
        [25] = "Aetherial Wheel",
        [26] = "Primed Aetherial Wheel",
        [27] = "Triple Triad Card",
        [28] = "Airship Component",
        [29] = "Currency",
        [30] = "Folklore Book",
        [31] = "Soul Crystal",
        [32] = "Orchestrion Roll",
        [33] = "Aquarium Tank Trimming",
        [34] = "Painting",
        [35] = "Tales of Adventure Retainer",
        [36] = "Submersible Component",
        [37] = "Eureka Logos Action Ingredient",
        [38] = "Bozja Mettle",
        [39] = "Bozja Lost Action",
        [40] = "Bozjan Cluster",
        [41] = "Unused",
        [42] = "Unused",
        [43] = "Placeholder Item",
        [44] = "Belts",
        [45] = "Archive Item",
        [46] = "Unused",
        [47] = "Sanctuary Cowrie",
        [48] = "Sanctuary Material",
        [49] = "Adventurer's Parcel",
        [50] = "Cosmic Exploration Material",
        [51] = "Outfit",
        [52] = "Occult Crescent Knowledge",
        [53] = "Occult Crescent Phantom Experience",
        [54] = "Occult Crescent Enlightenment Piece",
        [55] = "Cosmic Exploration Cosmocredit",
        [56] = "Cosmic Exploration Lunar Credit",
        [57] = "Occult Crescent Sanguine Cipher",
    };

    public static string? GetFilterGroupLabel(byte filterGroup)
    {
        return FilterGroupLabels.TryGetValue(filterGroup, out var label)
            ? label
            : null;
    }

    public static bool IsEquipmentFilterGroup(byte filterGroup)
    {
        return filterGroup is 1 or 2 or 3 or 4 or 44;
    }

    public static string GetPrimaryCategoryLabel(byte? filterGroupId, string? filterGroupLabel, string? equipSlotCategoryLabel)
    {
        if (filterGroupId is byte filterGroup && IsEquipmentFilterGroup(filterGroup) && !string.IsNullOrWhiteSpace(equipSlotCategoryLabel))
        {
            return equipSlotCategoryLabel;
        }

        if (!string.IsNullOrWhiteSpace(filterGroupLabel))
        {
            return filterGroupLabel;
        }

        return "Unknown";
    }
}
