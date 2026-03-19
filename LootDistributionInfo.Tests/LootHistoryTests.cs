using Xunit;

namespace LootDistributionInfo.Tests;

public sealed class LootHistoryTests
{
    [Fact]
    public void TryAdd_SkipsDuplicateLinesAcrossSourcesWithinWindow()
    {
        var history = new LootHistory();
        var first = LootMatcher.TryMatch("You obtain 368 gil.", LootCaptureSource.ChatMessage, DateTimeOffset.Parse("2026-03-11T12:00:00Z"))!;
        var duplicate = LootMatcher.TryMatch("You obtain 368 gil.", LootCaptureSource.LogMessage, DateTimeOffset.Parse("2026-03-11T12:00:01Z"))!;

        var firstAdded = history.TryAdd(first, 500, TimeSpan.FromSeconds(2));
        var duplicateAdded = history.TryAdd(duplicate, 500, TimeSpan.FromSeconds(2));

        Assert.True(firstAdded);
        Assert.False(duplicateAdded);
        Assert.Single(history.Records);
    }

    [Fact]
    public void Trim_RemovesOldestEntriesBeyondCap()
    {
        var history = new LootHistory();

        for (var i = 0; i < 3; i++)
        {
            history.TryAdd(
                LootMatcher.TryMatch($"You obtain {i + 1} gil.", LootCaptureSource.ChatMessage, DateTimeOffset.Parse($"2026-03-11T12:00:0{i}Z"))!,
                2,
                TimeSpan.FromSeconds(2));
        }

        Assert.Equal(2, history.Records.Count);
        Assert.Equal("You obtain 3 gil.", history.Records[0].RawText);
        Assert.Equal("You obtain 2 gil.", history.Records[1].RawText);
    }

    [Fact]
    public void Clear_RemovesAllRecords()
    {
        var history = new LootHistory([
            LootMatcher.TryMatch("You obtain 368 gil.", LootCaptureSource.ChatMessage, DateTimeOffset.Parse("2026-03-11T12:00:00Z"))!,
        ]);

        history.Clear();

        Assert.Empty(history.Records);
    }
}
