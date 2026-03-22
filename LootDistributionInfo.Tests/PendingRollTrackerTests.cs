using Xunit;

namespace LootDistributionInfo.Tests;

public sealed class PendingRollTrackerTests
{
    [Fact]
    public void TryResolve_AttachesOldestMatchingSessionWithinWindow()
    {
        var tracker = new PendingRollTracker();
        tracker.AddRoll(CreateRoll("Alice Example", LootRollType.Need, 87, "animal skins", "2026-03-11T12:00:00Z"), "Gridania", TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(2));
        tracker.AddRoll(CreateRoll("Bob Example", LootRollType.Greed, 41, "animal skins", "2026-03-11T12:00:01Z"), "Gridania", TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(2));

        var resolved = tracker.TryResolve("animal skins", "Gridania", DateTimeOffset.Parse("2026-03-11T12:00:03Z"), TimeSpan.FromMinutes(5));

        Assert.NotNull(resolved);
        Assert.Equal(2, resolved!.Entries.Count);
        Assert.True(resolved.Resolved);
        Assert.Equal("Alice Example Need 87; Bob Example Greed 41", resolved.ToSummaryText());
    }

    [Fact]
    public void TryResolve_DoesNotCrossMatchDifferentZones()
    {
        var tracker = new PendingRollTracker();
        tracker.AddRoll(CreateRoll("Alice Example", LootRollType.Need, 87, "animal skins", "2026-03-11T12:00:00Z"), "Gridania", TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(2));

        var resolved = tracker.TryResolve("animal skins", "Limsa Lominsa", DateTimeOffset.Parse("2026-03-11T12:00:03Z"), TimeSpan.FromMinutes(5));

        Assert.Null(resolved);
    }

    [Fact]
    public void AddRoll_SkipsDuplicateRawLinesWithinDedupeWindow()
    {
        var tracker = new PendingRollTracker();
        var first = tracker.AddRoll(CreateRoll("Alice Example", LootRollType.Need, 87, "animal skins", "2026-03-11T12:00:00Z"), "Gridania", TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(2));
        var duplicate = tracker.AddRoll(CreateRoll("Alice Example", LootRollType.Need, 87, "animal skins", "2026-03-11T12:00:01Z"), "Gridania", TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(2));

        Assert.Equal(PendingRollAddResult.CreatedSession, first);
        Assert.Equal(PendingRollAddResult.Duplicate, duplicate);
        Assert.Single(tracker.Sessions);
        Assert.Single(tracker.Sessions[0].Entries);
    }

    private static LootRollRecord CreateRoll(string playerName, LootRollType type, int? rollValue, string itemName, string capturedAt)
    {
        var rawText = type switch
        {
            LootRollType.Pass => $"{playerName} passes on {itemName}.",
            _ => $"{playerName} rolls {type} {rollValue} on {itemName}.",
        };

        return new LootRollRecord
        {
            CapturedAtUtc = DateTimeOffset.Parse(capturedAt),
            PlayerName = playerName,
            RollType = type,
            RollValue = rollValue,
            ItemName = itemName,
            RawText = rawText,
        };
    }
}
