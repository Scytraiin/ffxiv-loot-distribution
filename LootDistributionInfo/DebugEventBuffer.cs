using System;
using System.Collections.Generic;

namespace LootDistributionInfo;

public sealed class DebugEventBuffer
{
    private readonly List<DebugEventRecord> records = [];
    private readonly int maxEntries;

    public DebugEventBuffer(int maxEntries = 500)
    {
        this.maxEntries = Math.Max(1, maxEntries);
    }

    public IReadOnlyList<DebugEventRecord> Records => this.records;

    public void Add(string area, string eventName, string details)
    {
        this.records.Insert(0, new DebugEventRecord
        {
            CapturedAtUtc = DateTimeOffset.UtcNow,
            Area = area,
            Event = eventName,
            Details = details,
        });

        if (this.records.Count > this.maxEntries)
        {
            this.records.RemoveRange(this.maxEntries, this.records.Count - this.maxEntries);
        }
    }

    public void Clear()
    {
        this.records.Clear();
    }
}
