using System;
using System.Collections.Generic;

namespace LootDistributionInfo;

public sealed class LootHistory
{
    private readonly List<LootRecord> records = [];

    public LootHistory(IEnumerable<LootRecord>? initialRecords = null)
    {
        if (initialRecords is null)
        {
            return;
        }

        this.records.AddRange(initialRecords);
    }

    public IReadOnlyList<LootRecord> Records => this.records;

    public void Clear()
    {
        this.records.Clear();
    }

    public bool TryAdd(LootRecord record, int maxEntries, TimeSpan dedupeWindow)
    {
        if (this.IsDuplicate(record, dedupeWindow))
        {
            return false;
        }

        // The list is stored newest-first so the window can render without extra sorting work.
        this.records.Insert(0, record);
        this.Trim(maxEntries);
        return true;
    }

    public void Trim(int maxEntries)
    {
        var normalizedMaxEntries = Math.Max(1, maxEntries);
        if (this.records.Count > normalizedMaxEntries)
        {
            this.records.RemoveRange(normalizedMaxEntries, this.records.Count - normalizedMaxEntries);
        }
    }

    public List<LootRecord> Snapshot()
    {
        return [.. this.records];
    }

    private bool IsDuplicate(LootRecord candidate, TimeSpan dedupeWindow)
    {
        var normalizedCandidate = LootMatcher.NormalizeForMatch(candidate.RawText);

        foreach (var existingRecord in this.records)
        {
            var ageDelta = (existingRecord.CapturedAtUtc - candidate.CapturedAtUtc).Duration();
            if (ageDelta > dedupeWindow)
            {
                continue;
            }

            if (LootMatcher.NormalizeForMatch(existingRecord.RawText) == normalizedCandidate)
            {
                return true;
            }
        }

        return false;
    }
}
