using System;
using System.Collections.Generic;
using System.Linq;

namespace LootDistributionInfo;

public sealed class LootOverviewSummary
{
    public int TotalEntries { get; init; }

    public int UniqueItems { get; init; }

    public DateTimeOffset? LatestItemAtUtc { get; init; }

    public IReadOnlyList<LootOverviewBucket> TopZones { get; init; } = [];

    public IReadOnlyList<LootOverviewBucket> TopCategories { get; init; } = [];

    public IReadOnlyList<LootOverviewBucket> TopItems { get; init; } = [];

    public IReadOnlyList<LootOverviewBucket> RarityBreakdown { get; init; } = [];

    public static LootOverviewSummary Build(IEnumerable<LootRecord> records)
    {
        var recordList = records.ToList();
        if (recordList.Count == 0)
        {
            return new LootOverviewSummary();
        }

        return new LootOverviewSummary
        {
            TotalEntries = recordList.Count,
            UniqueItems = recordList
                .Select(GetUniqueItemKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count(),
            LatestItemAtUtc = recordList.MaxBy(record => record.CapturedAtUtc)?.CapturedAtUtc,
            TopZones = BuildBuckets(recordList, record => string.IsNullOrWhiteSpace(record.ZoneName) ? "Unknown" : record.ZoneName),
            TopCategories = BuildBuckets(recordList, record => string.IsNullOrWhiteSpace(record.ItemCategoryLabel) ? "Unknown" : record.ItemCategoryLabel),
            TopItems = BuildBuckets(recordList, GetDisplayItemName),
            RarityBreakdown = BuildBuckets(recordList, record => GetRarityLabel(record.Rarity), limit: 10),
        };
    }

    private static IReadOnlyList<LootOverviewBucket> BuildBuckets(IEnumerable<LootRecord> records, Func<LootRecord, string> selector, int limit = 5)
    {
        return records
            .GroupBy(selector, StringComparer.OrdinalIgnoreCase)
            .Select(group => new LootOverviewBucket(group.First(), group.Key, group.Count()))
            .OrderByDescending(bucket => bucket.Count)
            .ThenBy(bucket => bucket.Label, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();
    }

    private static string GetUniqueItemKey(LootRecord record)
    {
        return LootItemKey.Build(record)
            ?? $"text:{GetDisplayItemName(record).Trim().ToLowerInvariant()}";
    }

    private static string GetDisplayItemName(LootRecord record)
    {
        return record.ResolvedItemName
            ?? record.ItemName
            ?? record.RawText;
    }

    private static string GetRarityLabel(uint? rarity)
    {
        return rarity switch
        {
            1 => "Common",
            2 => "Uncommon",
            3 => "Rare",
            4 => "Relic",
            7 => "Aetherial",
            _ => "Unknown",
        };
    }
}

public sealed record LootOverviewBucket(LootRecord SampleRecord, string Label, int Count);
