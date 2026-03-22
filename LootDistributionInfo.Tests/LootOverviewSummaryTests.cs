using Xunit;

namespace LootDistributionInfo.Tests;

public sealed class LootOverviewSummaryTests
{
    [Fact]
    public void Build_ComputesTopBucketsAndUniqueItems()
    {
        var summary = LootOverviewSummary.Build(
        [
            CreateRecord("Animal Skin", 100, 2, "North Shroud", "Crafting Material", "2026-03-22T10:00:00Z"),
            CreateRecord("Animal Skin", 100, 2, "North Shroud", "Crafting Material", "2026-03-22T11:00:00Z"),
            CreateRecord("Potion", 200, 1, "Gridania", "Potion", "2026-03-22T12:00:00Z"),
        ]);

        Assert.Equal(3, summary.TotalEntries);
        Assert.Equal(2, summary.UniqueItems);
        Assert.Equal("North Shroud", summary.TopZones[0].Label);
        Assert.Equal(2, summary.TopZones[0].Count);
        Assert.Equal("Crafting Material", summary.TopCategories[0].Label);
        Assert.Equal("Animal Skin", summary.TopItems[0].Label);
        Assert.Equal("Uncommon", summary.RarityBreakdown[0].Label);
        Assert.Equal(DateTimeOffset.Parse("2026-03-22T12:00:00Z"), summary.LatestItemAtUtc);
    }

    [Fact]
    public void Build_UsesTextFallbackWhenItemIdIsMissing()
    {
        var summary = LootOverviewSummary.Build(
        [
            new LootRecord
            {
                CapturedAtUtc = DateTimeOffset.Parse("2026-03-22T10:00:00Z"),
                LootText = "Unresolved Item",
                RawText = "You obtain Unresolved Item.",
                ZoneName = "Unknown",
            },
            new LootRecord
            {
                CapturedAtUtc = DateTimeOffset.Parse("2026-03-22T11:00:00Z"),
                LootText = "Unresolved Item",
                RawText = "You obtain Unresolved Item.",
                ZoneName = "Unknown",
            },
        ]);

        Assert.Equal(1, summary.UniqueItems);
        Assert.Equal("Unresolved Item", summary.TopItems[0].Label);
    }

    private static LootRecord CreateRecord(string lootText, uint itemId, uint rarity, string zone, string category, string capturedAt)
    {
        return new LootRecord
        {
            CapturedAtUtc = DateTimeOffset.Parse(capturedAt),
            LootText = lootText,
            ResolvedItemName = lootText,
            RawText = $"You obtain {lootText}.",
            ItemId = itemId,
            Rarity = rarity,
            ZoneName = zone,
            ItemCategoryLabel = category,
        };
    }
}
