using System;
using System.Collections.Generic;
using System.Linq;

namespace LootDistributionInfo;

public sealed class PendingRollTracker
{
    private readonly List<PendingRollSession> sessions = [];
    private readonly TimeSpan sessionWindow;

    public PendingRollTracker(TimeSpan? sessionWindow = null)
    {
        this.sessionWindow = sessionWindow ?? TimeSpan.FromMinutes(5);
    }

    public PendingRollSession AddOrAppend(LootRollObservation observation)
    {
        var existingSession = this.sessions
            .Where(session => string.Equals(session.ItemKey, observation.ItemKey, StringComparison.Ordinal)
                && string.Equals(session.ZoneName, observation.ZoneName, StringComparison.OrdinalIgnoreCase)
                && observation.CapturedAtUtc - session.LastSeenUtc <= this.sessionWindow)
            .OrderBy(session => session.FirstSeenUtc)
            .FirstOrDefault();

        if (existingSession is null)
        {
            existingSession = new PendingRollSession(observation.ItemKey, observation.ItemName, observation.ZoneName, observation.CapturedAtUtc);
            this.sessions.Add(existingSession);
        }

        existingSession.Add(observation.Entry, observation.CapturedAtUtc, observation.ItemName);
        this.sessions.Sort((left, right) => left.FirstSeenUtc.CompareTo(right.FirstSeenUtc));
        return existingSession;
    }

    public IReadOnlyList<LootRollEntry>? TryResolve(string itemKey, string zoneName, DateTimeOffset capturedAtUtc)
    {
        var matchingSession = this.sessions
            .Where(session => string.Equals(session.ItemKey, itemKey, StringComparison.Ordinal)
                && string.Equals(session.ZoneName, zoneName, StringComparison.OrdinalIgnoreCase)
                && capturedAtUtc - session.LastSeenUtc <= this.sessionWindow)
            .OrderBy(session => session.FirstSeenUtc)
            .FirstOrDefault();

        if (matchingSession is null)
        {
            return null;
        }

        this.sessions.Remove(matchingSession);
        return matchingSession.Entries
            .OrderBy(entry => entry.CapturedAtUtc)
            .ToList();
    }

    public IReadOnlyList<PendingRollSession> ExpireOlderThan(DateTimeOffset thresholdUtc)
    {
        var expired = this.sessions
            .Where(session => session.LastSeenUtc < thresholdUtc)
            .OrderBy(session => session.FirstSeenUtc)
            .ToList();

        if (expired.Count == 0)
        {
            return [];
        }

        foreach (var session in expired)
        {
            this.sessions.Remove(session);
        }

        return expired;
    }
}

public sealed class PendingRollSession
{
    private readonly List<LootRollEntry> entries = [];

    public PendingRollSession(string itemKey, string itemName, string zoneName, DateTimeOffset capturedAtUtc)
    {
        this.ItemKey = itemKey;
        this.ItemName = itemName;
        this.ZoneName = zoneName;
        this.FirstSeenUtc = capturedAtUtc;
        this.LastSeenUtc = capturedAtUtc;
    }

    public string ItemKey { get; }

    public string ItemName { get; private set; }

    public string ZoneName { get; }

    public DateTimeOffset FirstSeenUtc { get; }

    public DateTimeOffset LastSeenUtc { get; private set; }

    public IReadOnlyList<LootRollEntry> Entries => this.entries;

    public void Add(LootRollEntry entry, DateTimeOffset capturedAtUtc, string itemName)
    {
        if (this.entries.Any(existingEntry =>
                string.Equals(existingEntry.PlayerDisplayName, entry.PlayerDisplayName, StringComparison.OrdinalIgnoreCase)
                && existingEntry.RollType == entry.RollType
                && existingEntry.RollValue == entry.RollValue
                && (existingEntry.CapturedAtUtc - entry.CapturedAtUtc).Duration() <= TimeSpan.FromSeconds(2)))
        {
            this.LastSeenUtc = capturedAtUtc;
            return;
        }

        this.entries.Add(entry);
        this.LastSeenUtc = capturedAtUtc;
        if (!string.IsNullOrWhiteSpace(itemName))
        {
            this.ItemName = itemName;
        }
    }
}

public sealed record LootRollObservation(
    LootRollEntry Entry,
    string ItemKey,
    string ItemName,
    string ZoneName,
    DateTimeOffset CapturedAtUtc,
    string RawText,
    uint? LogMessageId);
