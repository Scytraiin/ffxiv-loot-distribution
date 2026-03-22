using Xunit;

namespace LootDistributionInfo.Tests;

public sealed class LootRollMatcherTests
{
    [Theory]
    [InlineData("Player Xavier rolls Need 87 on a loot item.", "Player Xavier", LootRollType.Need, 87, "loot item")]
    [InlineData("Alliance Member rolls Greed 41 on loot item.", "Alliance Member", LootRollType.Greed, 41, "loot item")]
    [InlineData("Other Person passes on a loot item.", "Other Person", LootRollType.Pass, null, "loot item")]
    public void TryMatch_AcceptsRollLines(string input, string expectedPlayer, LootRollType expectedType, int? expectedValue, string expectedItem)
    {
        var result = LootRollMatcher.TryMatch(input);

        Assert.NotNull(result);
        Assert.Equal(expectedPlayer, result!.PlayerName);
        Assert.Equal(expectedType, result.RollType);
        Assert.Equal(expectedValue, result.RollValue);
        Assert.Equal(expectedItem, result.ItemName);
    }

    [Theory]
    [InlineData("You obtain 368 gil.")]
    [InlineData("Party Member: focus on the mechanic.")]
    [InlineData("This system line is only about a duty pop.")]
    public void TryMatch_RejectsNonRollLines(string input)
    {
        Assert.Null(LootRollMatcher.TryMatch(input));
    }
}
