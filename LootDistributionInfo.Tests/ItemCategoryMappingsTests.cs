using Xunit;

namespace LootDistributionInfo.Tests;

public sealed class ItemCategoryMappingsTests
{
    [Theory]
    [InlineData((byte)1, "Physical Weapon")]
    [InlineData((byte)5, "Meal")]
    [InlineData((byte)12, "Crafting Material")]
    [InlineData((byte)29, "Currency")]
    [InlineData((byte)44, "Belts")]
    [InlineData((byte)47, "Sanctuary Cowrie")]
    [InlineData((byte)50, "Cosmic Exploration Material")]
    [InlineData((byte)57, "Occult Crescent Sanguine Cipher")]
    public void GetFilterGroupLabel_ReturnsExpectedLabels(byte filterGroup, string expectedLabel)
    {
        Assert.Equal(expectedLabel, ItemCategoryMappings.GetFilterGroupLabel(filterGroup));
    }

    [Fact]
    public void GetPrimaryCategoryLabel_PrefersEquipSlotForEquipmentGroups()
    {
        var category = ItemCategoryMappings.GetPrimaryCategoryLabel(4, "Gear", "Head Equipment");

        Assert.Equal("Head Equipment", category);
    }

    [Fact]
    public void GetPrimaryCategoryLabel_FallsBackToFilterGroupForNonEquipmentGroups()
    {
        var category = ItemCategoryMappings.GetPrimaryCategoryLabel(29, "Currency", "Head Equipment");

        Assert.Equal("Currency", category);
    }

    [Fact]
    public void GetPrimaryCategoryLabel_UsesUnknownWhenNoClassificationExists()
    {
        var category = ItemCategoryMappings.GetPrimaryCategoryLabel(null, null, null);

        Assert.Equal("Unknown", category);
    }
}
