using Xunit;

namespace LootDistributionInfo.Tests;

public sealed class LootMatcherTests
{
    [Theory]
    [InlineData("You obtain 368 gil.", "You", 368, "gil")]
    [InlineData("You obtain a bottle of desert saffron.", "You", 1, "bottle of desert saffron")]
    [InlineData("You obtained a bottle of desert saffron.", "You", 1, "bottle of desert saffron")]
    [InlineData("Player Xavier obtains a loot item.", "Player Xavier", 1, "loot item")]
    [InlineData("Alliance Member obtained 2 sacks of nuts.", "Alliance Member", 2, "sacks of nuts")]
    public void TryMatch_AcceptsLootLikeLines(string input, string expectedSubject, int expectedQuantity, string expectedItem)
    {
        var parsed = LootMatcher.TryMatch(input);

        Assert.NotNull(parsed);
        Assert.Equal(expectedSubject, parsed!.SubjectText);
        Assert.Equal(expectedQuantity, parsed.Quantity);
        Assert.Equal(expectedItem, parsed.ItemName);
    }

    [Theory]
    [InlineData("Party Member: focus on the mechanic.")]
    [InlineData("This system line is only about a duty pop.")]
    [InlineData("The word unobtained appears here, but no loot was gained.")]
    [InlineData("You obtain")]
    [InlineData("Player Xavier obtains")]
    public void TryMatch_RejectsNonLootLines(string input)
    {
        var result = LootMatcher.TryMatch(input);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("YOU OBTAIN A REWARD.")]
    [InlineData("You obtain a reward!")]
    [InlineData("You obtained a reward?")]
    public void ContainsLootVerb_HandlesCaseAndPunctuation(string input)
    {
        Assert.True(LootMatcher.ContainsLootVerb(input));
    }

    [Fact]
    public void TryMatch_KeepsTwoWordNameWhenItCannotBeVerified()
    {
        var result = LootMatcher.TryMatch("Other Person obtains a reward.");

        Assert.NotNull(result);
        Assert.Equal("Other Person", result!.SubjectText);
        Assert.Equal(1, result.Quantity);
        Assert.Equal("reward", result.ItemName);
    }

    [Fact]
    public void TryMatch_LeavesWhoEmptyWhenPrefixIsNotACharacterName()
    {
        var result = LootMatcher.TryMatch("Treasure coffer obtains a reward.");

        Assert.NotNull(result);
        Assert.Equal("Treasure coffer", result!.SubjectText);
        Assert.Equal(1, result.Quantity);
        Assert.Equal("reward", result.ItemName);
    }
}
