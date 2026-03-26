using Xunit;

namespace LootDistributionInfo.Tests;

public sealed class LootRecordTests
{
    [Fact]
    public void Normalize_MigratesLegacyLootTextIntoQuantityAndItemName()
    {
        var record = new LootRecord
        {
#pragma warning disable CS0618
            LootText = "2 animal skins",
#pragma warning restore CS0618
        };

        record.Normalize();

        Assert.Equal(2, record.Quantity);
        Assert.Equal("animal skins", record.ItemName);
    }
}
