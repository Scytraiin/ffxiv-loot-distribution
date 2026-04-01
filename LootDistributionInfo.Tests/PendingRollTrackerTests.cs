using System;
using System.Linq;

using Xunit;

namespace LootDistributionInfo.Tests;

public sealed class PendingRollTrackerTests
{
    [Fact]
    public void AddOrAppend_AndResolve_UsesOldestCompatibleSession()
    {
        var tracker = new PendingRollTracker(TimeSpan.FromMinutes(5));
        tracker.AddOrAppend(CreateObservation("item:1001", "Animal Skin", "North Shroud", "Party Friend", LootRollType.Need, 87, "2026-04-01T12:00:00Z"));
        tracker.AddOrAppend(CreateObservation("item:1001", "Animal Skin", "North Shroud", "Other Person", LootRollType.Greed, 41, "2026-04-01T12:00:05Z"));

        var resolved = tracker.TryResolve("item:1001", "North Shroud", DateTimeOffset.Parse("2026-04-01T12:00:08Z"));

        Assert.NotNull(resolved);
        Assert.Equal(2, resolved!.Count);
        Assert.Equal(["Party Friend", "Other Person"], resolved.Select(entry => entry.PlayerName));
    }

    [Fact]
    public void ExpireOlderThan_RemovesExpiredSessions()
    {
        var tracker = new PendingRollTracker(TimeSpan.FromMinutes(5));
        tracker.AddOrAppend(CreateObservation("item:1001", "Animal Skin", "North Shroud", "Party Friend", LootRollType.Need, 87, "2026-04-01T12:00:00Z"));

        var expired = tracker.ExpireOlderThan(DateTimeOffset.Parse("2026-04-01T12:06:00Z"));

        Assert.Single(expired);
        Assert.Null(tracker.TryResolve("item:1001", "North Shroud", DateTimeOffset.Parse("2026-04-01T12:06:01Z")));
    }

    private static LootRollObservation CreateObservation(
        string itemKey,
        string itemName,
        string zoneName,
        string playerName,
        LootRollType rollType,
        int? rollValue,
        string capturedAtUtc)
    {
        return new LootRollObservation(
            new LootRollEntry
            {
                CapturedAtUtc = DateTimeOffset.Parse(capturedAtUtc),
                PlayerName = playerName,
                PlayerDisplayName = playerName,
                RollType = rollType,
                RollValue = rollValue,
                ItemKey = itemKey,
                ItemName = itemName,
            },
            itemKey,
            itemName,
            zoneName,
            DateTimeOffset.Parse(capturedAtUtc),
            $"{playerName} roll",
            null);
    }
}
