using System;

using Xunit;

namespace LootDistributionInfo.Tests;

public sealed class LootRollExtractorTests
{
    [Fact]
    public void TryExtract_UsesStructuredCandidatesForNeedRoll()
    {
        var result = LootRollExtractor.TryExtract(
            "Party Friend rolls Need 87 on Animal Skin.",
            new LootRollExtractionContext(
                [new LootRollPlayerCandidate("Party Friend", "Party Friend (Spriggan)", 90, "Spriggan")],
                [new LootRollItemCandidate("Animal Skin", 1001u)],
                [87],
                33,
                6001u),
            "North Shroud",
            new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero));

        Assert.NotNull(result);
        Assert.Equal(LootRollType.Need, result!.Entry.RollType);
        Assert.Equal(87, result.Entry.RollValue);
        Assert.Equal("Party Friend", result.Entry.PlayerName);
        Assert.Equal("Party Friend (Spriggan)", result.Entry.PlayerDisplayName);
        Assert.Equal("item:1001", result.ItemKey);
    }

    [Fact]
    public void TryExtract_HandlesPassWithoutNumericValue()
    {
        var result = LootRollExtractor.TryExtract(
            "Other Person passes on Bottle of Desert Saffron.",
            new LootRollExtractionContext(
                [new LootRollPlayerCandidate("Other Person", "Other Person", null, null)],
                [new LootRollItemCandidate("Bottle of Desert Saffron", null)],
                [],
                33,
                null),
            "North Shroud",
            new DateTimeOffset(2026, 4, 1, 12, 1, 0, TimeSpan.Zero));

        Assert.NotNull(result);
        Assert.Equal(LootRollType.Pass, result!.Entry.RollType);
        Assert.Null(result.Entry.RollValue);
        Assert.Equal("name:bottle of desert saffron", result.ItemKey);
    }

    [Fact]
    public void TryExtract_ReturnsNullWithoutUsableItemIdentity()
    {
        var result = LootRollExtractor.TryExtract(
            "Party Friend rolls Need 42.",
            new LootRollExtractionContext(
                [new LootRollPlayerCandidate("Party Friend", "Party Friend", null, null)],
                [],
                [42],
                33,
                6002u),
            "North Shroud",
            new DateTimeOffset(2026, 4, 1, 12, 2, 0, TimeSpan.Zero));

        Assert.Null(result);
    }
}
