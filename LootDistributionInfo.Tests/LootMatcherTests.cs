using Xunit;

namespace LootDistributionInfo.Tests;

public sealed class LootMatcherTests
{
    [Theory]
    [InlineData("You obtain 368 gil.", "Loot Tester", LootWhoConfidence.Self, "368 gil")]
    [InlineData("You obtain a bottle of desert saffron.", "Loot Tester", LootWhoConfidence.Self, "bottle of desert saffron")]
    [InlineData("You obtained a bottle of desert saffron.", "Loot Tester", LootWhoConfidence.Self, "bottle of desert saffron")]
    [InlineData("Player Xavier obtains a loot item.", "Player Xavier", LootWhoConfidence.PartyOrAllianceVerified, "loot item")]
    [InlineData("Alliance Member obtained 2 sacks of nuts.", "Alliance Member", LootWhoConfidence.PartyOrAllianceVerified, "2 sacks of nuts")]
    public void TryMatch_AcceptsLootLikeLines(string input, string expectedPlayer, LootWhoConfidence expectedConfidence, string expectedItem)
    {
        var result = LootMatcher.TryMatch(
            input,
            LootCaptureSource.ChatMessage,
            DateTimeOffset.Parse("2026-03-11T12:00:00Z"),
            "Loot Tester",
            ["Player Xavier", "Alliance Member"]);

        Assert.NotNull(result);
        Assert.Equal(expectedPlayer, result.WhoName);
        Assert.Equal(expectedConfidence, result.WhoConfidence);
        Assert.Equal(expectedItem, result.LootText);
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

    [Fact]
    public void TryMatch_KeepsTwoWordNameWhenItCannotBeVerified()
    {
        var result = LootMatcher.TryMatch(
            "Other Person obtains a reward.",
            LootCaptureSource.ChatMessage,
            DateTimeOffset.Parse("2026-03-11T12:00:00Z"),
            "Loot Tester",
            ["Party Friend"]);

        Assert.NotNull(result);
        Assert.Equal("Other Person", result.WhoName);
        Assert.Equal(LootWhoConfidence.TextOnly, result.WhoConfidence);
        Assert.Equal("reward", result.LootText);
    }

    [Fact]
    public void TryMatch_LeavesWhoEmptyWhenPrefixIsNotACharacterName()
    {
        var result = LootMatcher.TryMatch(
            "Treasure coffer obtains a reward.",
            LootCaptureSource.ChatMessage,
            DateTimeOffset.Parse("2026-03-11T12:00:00Z"),
            "Loot Tester",
            ["Party Friend"]);

        Assert.NotNull(result);
        Assert.Null(result.WhoName);
        Assert.Equal(LootWhoConfidence.Unknown, result.WhoConfidence);
        Assert.Equal("reward", result.LootText);
    }
}
