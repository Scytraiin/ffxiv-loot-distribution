using Xunit;

namespace LootDistributionInfo.Tests;

public sealed class LootQuantityParserTests
{
    [Theory]
    [InlineData("2 animal skins", 2, "animal skins")]
    [InlineData("368 gil", 368, "gil")]
    [InlineData("bottle of desert saffron", 1, "bottle of desert saffron")]
    [InlineData("   17 cracked clusters   ", 17, "cracked clusters")]
    public void Split_ParsesQuantityWhenPresent(string input, int expectedQuantity, string expectedItemName)
    {
        var (quantity, itemName) = LootQuantityParser.Split(input);

        Assert.Equal(expectedQuantity, quantity);
        Assert.Equal(expectedItemName, itemName);
    }
}
