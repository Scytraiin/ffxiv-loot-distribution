using Xunit;

namespace LootDistributionInfo.Tests;

public sealed class LootHistoryTests
{
    [Fact]
    public void TryAdd_SkipsDuplicateLinesAcrossSourcesWithinWindow()
    {
        var history = new LootHistory();
        var first = CreateRecord("You obtain 368 gil.", LootCaptureSource.ChatMessage, "2026-03-11T12:00:00Z");
        var duplicate = CreateRecord("You obtain 368 gil.", LootCaptureSource.LogMessage, "2026-03-11T12:00:01Z");

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
                CreateRecord($"You obtain {i + 1} gil.", LootCaptureSource.ChatMessage, $"2026-03-11T12:00:0{i}Z"),
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
            CreateRecord("You obtain 368 gil.", LootCaptureSource.ChatMessage, "2026-03-11T12:00:00Z"),
        ]);

        history.Clear();

        Assert.Empty(history.Records);
    }

    [Fact]
    public void RemoveWhere_RemovesOnlyMatchingRecords()
    {
        var history = new LootHistory([
            CreateRecord("You obtain 368 gil.", LootCaptureSource.ChatMessage, "2026-03-11T12:00:00Z"),
            CreateRecord("You obtain 2 animal skins.", LootCaptureSource.ChatMessage, "2026-03-11T12:00:01Z"),
        ]);

        var removedCount = history.RemoveWhere(record => record.ItemName == "gil");

        Assert.Equal(1, removedCount);
        Assert.Single(history.Records);
        Assert.Equal("animal skins", history.Records[0].ItemName);
    }

    private static LootRecord CreateRecord(string rawText, LootCaptureSource source, string capturedAt)
    {
        var parsed = LootMatcher.TryMatch(rawText)!;
        return new LootRecord
        {
            CapturedAtUtc = DateTimeOffset.Parse(capturedAt),
            RawText = parsed.RawText,
            Quantity = parsed.Quantity,
            ItemName = parsed.ItemName,
            Source = source,
        };
    }
}
