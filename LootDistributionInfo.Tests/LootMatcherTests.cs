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
        var parsed = LootMatcher.TryMatch(input);

        Assert.NotNull(parsed);
        var (whoName, confidence) = ResolveWho(parsed!.SubjectText, "Loot Tester", ["Player Xavier", "Alliance Member"]);
        Assert.Equal(expectedPlayer, whoName);
        Assert.Equal(expectedConfidence, confidence);
        Assert.Equal(expectedItem, parsed.LootText);
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
        var (whoName, confidence) = ResolveWho(result!.SubjectText, "Loot Tester", ["Party Friend"]);
        Assert.Equal("Other Person", whoName);
        Assert.Equal(LootWhoConfidence.TextOnly, confidence);
        Assert.Equal("reward", result.LootText);
    }

    [Fact]
    public void TryMatch_LeavesWhoEmptyWhenPrefixIsNotACharacterName()
    {
        var result = LootMatcher.TryMatch("Treasure coffer obtains a reward.");

        Assert.NotNull(result);
        var (whoName, confidence) = ResolveWho(result!.SubjectText, "Loot Tester", ["Party Friend"]);
        Assert.Null(whoName);
        Assert.Equal(LootWhoConfidence.Unknown, confidence);
        Assert.Equal("reward", result.LootText);
    }

    private static (string? WhoName, LootWhoConfidence Confidence) ResolveWho(string? subjectText, string localPlayerName, string[] knownNames)
    {
        if (string.IsNullOrWhiteSpace(subjectText))
        {
            return (null, LootWhoConfidence.Unknown);
        }

        if (string.Equals(subjectText, "You", StringComparison.OrdinalIgnoreCase))
        {
            return (localPlayerName, LootWhoConfidence.Self);
        }

        if (!LootMatcher.LooksLikeTwoWordName(subjectText))
        {
            return (null, LootWhoConfidence.Unknown);
        }

        var normalizedCandidate = LootMatcher.NormalizeForNameMatch(subjectText);
        var normalizedKnownNames = knownNames.Select(LootMatcher.NormalizeForNameMatch).ToHashSet(StringComparer.Ordinal);
        var confidence = normalizedKnownNames.Contains(normalizedCandidate)
            ? LootWhoConfidence.PartyOrAllianceVerified
            : LootWhoConfidence.TextOnly;

        return (subjectText, confidence);
    }
}
