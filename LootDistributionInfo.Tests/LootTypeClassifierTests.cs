using Xunit;

namespace LootDistributionInfo.Tests;

public sealed class LootTypeClassifierTests
{
    [Theory]
    [InlineData(0u, LootTypeBucket.Other)]
    [InlineData(2u, LootTypeBucket.Dungeon)]
    [InlineData(5u, LootTypeBucket.Raid)]
    [InlineData(21u, LootTypeBucket.Raid)]
    [InlineData(27u, LootTypeBucket.Raid)]
    [InlineData(4u, LootTypeBucket.Other)]
    public void Classify_ReturnsExpectedBucket(uint contentTypeId, LootTypeBucket expected)
    {
        Assert.Equal(expected, LootTypeClassifier.Classify(contentTypeId));
    }
}
