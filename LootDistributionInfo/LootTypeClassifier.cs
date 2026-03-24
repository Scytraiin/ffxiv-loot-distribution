namespace LootDistributionInfo;

public static class LootTypeClassifier
{
    public static LootTypeBucket Classify(uint contentTypeId)
    {
        return contentTypeId switch
        {
            2 => LootTypeBucket.Dungeon,
            5 or 21 or 27 => LootTypeBucket.Raid,
            _ => LootTypeBucket.Other,
        };
    }
}
