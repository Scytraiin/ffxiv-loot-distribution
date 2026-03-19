using Xunit;

namespace LootDistributionInfo.Tests;

public sealed class LootMatcherTests
{
    [Theory]
    [InlineData("You obtain 368 gil.", "You", "368 gil")]
    [InlineData("You obtain a bottle of desert saffron.", "You", "bottle of desert saffron")]
    [InlineData("You obtained a bottle of desert saffron.", "You", "bottle of desert saffron")]
    [InlineData("Player X obtains a loot item.", "Player X", "loot item")]
    [InlineData("Alliance Member obtained 2 sacks of nuts.", "Alliance Member", "2 sacks of nuts")]
    public void TryMatch_AcceptsLootLikeLines(string input, string expectedPlayer, string expectedItem)
    {
        var result = LootMatcher.TryMatch(input, LootCaptureSource.ChatMessage, DateTimeOffset.Parse("2026-03-11T12:00:00Z"));

        Assert.NotNull(result);
        Assert.Equal(expectedPlayer, result.PlayerName);
        Assert.Equal(expectedItem, result.ItemText);
    }

    [Theory]
    [InlineData("Party Member: focus on the mechanic.")]
    [InlineData("This system line is only about a duty pop.")]
    [InlineData("The word unobtained appears here, but no loot was gained.")]
    public void TryMatch_RejectsNonLootLines(string input)
    {
        var result = LootMatcher.TryMatch(input, LootCaptureSource.ChatMessage, DateTimeOffset.Parse("2026-03-11T12:00:00Z"));

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
}
