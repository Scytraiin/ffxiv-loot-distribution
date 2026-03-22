using System;
using System.Collections.Generic;
using System.Linq;

namespace LootDistributionInfo;

public enum PendingRollAddResult
{
    Duplicate = 0,
    AddedToExistingSession = 1,
    CreatedSession = 2,
}

public sealed class PendingRollTracker
{
    private readonly List<PendingRollSession> sessions = [];

    public IReadOnlyList<PendingRollSession> Sessions => this.sessions;

    public PendingRollAddResult AddRoll(LootRollRecord roll, string zoneName, TimeSpan correlationWindow, TimeSpan dedupeWindow)
    {
        if (this.IsDuplicate(roll, dedupeWindow))
        {
            return PendingRollAddResult.Duplicate;
        }

        var normalizedItemName = LootRollMatcher.NormalizeItemKey(roll.ItemName);
        var existingSession = this.sessions
            .Where(session => !session.Resolved)
            .Where(session => string.Equals(session.ZoneName, zoneName, StringComparison.Ordinal))
            .Where(session => string.Equals(session.NormalizedItemName, normalizedItemName, StringComparison.Ordinal))
            .Where(session => (roll.CapturedAtUtc - session.LastSeenUtc).Duration() <= correlationWindow)
            .OrderByDescending(session => session.LastSeenUtc)
            .FirstOrDefault();

        if (existingSession is null)
        {
            this.sessions.Add(new PendingRollSession
            {
                NormalizedItemName = normalizedItemName,
                ZoneName = zoneName,
                FirstSeenUtc = roll.CapturedAtUtc,
                LastSeenUtc = roll.CapturedAtUtc,
                Entries = [roll],
            });

            return PendingRollAddResult.CreatedSession;
        }

        existingSession.Entries.Add(roll);
        existingSession.LastSeenUtc = roll.CapturedAtUtc;
        return PendingRollAddResult.AddedToExistingSession;
    }

    public PendingRollSession? TryResolve(string lootText, string zoneName, DateTimeOffset capturedAtUtc, TimeSpan correlationWindow)
    {
        var normalizedItemName = LootRollMatcher.NormalizeItemKey(lootText);
        var matchingSession = this.sessions
            .Where(session => !session.Resolved)
            .Where(session => string.Equals(session.ZoneName, zoneName, StringComparison.Ordinal))
            .Where(session => string.Equals(session.NormalizedItemName, normalizedItemName, StringComparison.Ordinal))
            .Where(session => capturedAtUtc >= session.FirstSeenUtc)
            .Where(session => capturedAtUtc - session.FirstSeenUtc <= correlationWindow)
            .OrderBy(session => session.FirstSeenUtc)
            .FirstOrDefault();

        if (matchingSession is null)
        {
            return null;
        }

        matchingSession.Resolved = true;
        return matchingSession;
    }

    public List<PendingRollSession> Expire(DateTimeOffset now, TimeSpan correlationWindow)
    {
        var expired = this.sessions
            .Where(session => !session.Resolved)
            .Where(session => now - session.LastSeenUtc > correlationWindow)
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

    private bool IsDuplicate(LootRollRecord candidate, TimeSpan dedupeWindow)
    {
        var normalizedRawText = LootMatcher.NormalizeForMatch(candidate.RawText);

        return this.sessions
            .SelectMany(session => session.Entries)
            .Any(existing =>
                (existing.CapturedAtUtc - candidate.CapturedAtUtc).Duration() <= dedupeWindow
                && LootMatcher.NormalizeForMatch(existing.RawText) == normalizedRawText);
    }
}
