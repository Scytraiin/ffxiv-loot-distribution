using System;
using System.Collections.Generic;
using System.Linq;

namespace LootDistributionInfo;

[Serializable]
public sealed class PendingRollSession
{
    public string NormalizedItemName { get; set; } = string.Empty;

    public string ZoneName { get; set; } = string.Empty;

    public DateTimeOffset FirstSeenUtc { get; set; }

    public DateTimeOffset LastSeenUtc { get; set; }

    public List<LootRollRecord> Entries { get; set; } = [];

    public bool Resolved { get; set; }

    public void Normalize()
    {
        this.NormalizedItemName = this.NormalizedItemName?.Trim() ?? string.Empty;
        this.ZoneName = this.ZoneName?.Trim() ?? string.Empty;
        this.Entries ??= [];

        foreach (var entry in this.Entries)
        {
            entry.Normalize();
        }
    }

    public string ToSummaryText()
    {
        return this.Entries.Count == 0
            ? LootRecord.NoRollsLabel
            : string.Join("; ", this.Entries.Select(entry => entry.ToSummaryText()));
    }
}
