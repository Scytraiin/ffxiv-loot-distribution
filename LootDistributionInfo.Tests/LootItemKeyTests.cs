using Xunit;

namespace LootDistributionInfo.Tests;

public sealed class LootItemKeyTests
{
    [Fact]
    public void Build_UsesResolvedItemIdWhenAvailable()
    {
        var record = new LootRecord
        {
            ItemId = 1234u,
            ItemName = "Animal Skin",
        };

        Assert.Equal("item:1234", LootItemKey.Build(record));
    }

    [Fact]
    public void Build_FallsBackToNormalizedItemName()
    {
        var record = new LootRecord
        {
            ItemName = "Bottle of Desert Saffron",
        };

        Assert.Equal("name:bottle of desert saffron", LootItemKey.Build(record));
    }
}
