using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

namespace LootDistributionInfo.Tests;

public sealed class LootHistoryBrowserTests
{
    [Fact]
    public void FilterAndSort_FavoritesQuickFilter_OnlyReturnsFavoritedResolvedItems()
    {
        var records = new[]
        {
            CreateRecord("Animal Skin", itemId: 1001u),
            CreateRecord("Potion", itemId: 1002u),
            CreateRecord("Unresolved", itemId: null),
        };

        var visible = LootHistoryBrowser.FilterAndSort(
            records,
            new LootHistoryBrowseOptions(
                string.Empty,
                LootHistoryQuickFilter.Favorites,
                LootHistorySortMode.NewestFirst,
                LootRecipientFilter.All,
                string.Empty,
                string.Empty,
                new HashSet<uint> { 1002u }));

        Assert.Single(visible);
        Assert.Equal("Potion", visible[0].ItemName);
    }

    [Fact]
    public void FilterAndSort_SelfQuickFilter_OnlyReturnsSelfLoot()
    {
        var records = new[]
        {
            CreateRecord("Animal Skin", confidence: LootWhoConfidence.Self),
            CreateRecord("Potion", confidence: LootWhoConfidence.PartyOrAllianceVerified),
        };

        var visible = LootHistoryBrowser.FilterAndSort(
            records,
            new LootHistoryBrowseOptions(
                string.Empty,
                LootHistoryQuickFilter.Self,
                LootHistorySortMode.NewestFirst,
                LootRecipientFilter.All,
                string.Empty,
                string.Empty,
                Array.Empty<uint>()));

        Assert.Single(visible);
        Assert.Equal(LootWhoConfidence.Self, visible[0].WhoConfidence);
    }

    [Fact]
    public void FilterAndSort_QuantityHighToLow_SortsByQuantityDescending()
    {
        var records = new[]
        {
            CreateRecord("Animal Skin", quantity: 2),
            CreateRecord("Potion", quantity: 9),
            CreateRecord("Crystal", quantity: 4),
        };

        var visible = LootHistoryBrowser.FilterAndSort(
            records,
            new LootHistoryBrowseOptions(
                string.Empty,
                LootHistoryQuickFilter.All,
                LootHistorySortMode.QuantityHighToLow,
                LootRecipientFilter.All,
                string.Empty,
                string.Empty,
                Array.Empty<uint>()));

        Assert.Equal(["Potion", "Crystal", "Animal Skin"], visible.Select(record => record.ItemName));
    }

    [Fact]
    public void Group_ByZone_CreatesStableZoneBuckets()
    {
        var records = new[]
        {
            CreateRecord("Animal Skin", zone: "North Shroud"),
            CreateRecord("Potion", zone: "North Shroud"),
            CreateRecord("Crystal", zone: "Limsa Lominsa"),
        };

        var groups = LootHistoryBrowser.Group(records, LootHistoryGroupingMode.ByZone);

        Assert.Equal(2, groups.Count);
        Assert.Equal("North Shroud", groups[0].Label);
        Assert.Equal(2, groups[0].Records.Count);
        Assert.Equal("Limsa Lominsa", groups[1].Label);
        Assert.Single(groups[1].Records);
    }

    [Fact]
    public void Group_ByRecipient_UsesDisplayNameWhenAvailable()
    {
        var records = new[]
        {
            CreateRecord("Animal Skin", whoName: "Party Friend", whoDisplayName: "Party Friend (Spriggan)"),
            CreateRecord("Potion", whoName: "Party Friend", whoDisplayName: "Party Friend (Spriggan)"),
        };

        var groups = LootHistoryBrowser.Group(records, LootHistoryGroupingMode.ByRecipient);

        Assert.Single(groups);
        Assert.Equal("Party Friend (Spriggan)", groups[0].Label);
    }

    private static LootRecord CreateRecord(
        string itemName,
        int quantity = 1,
        string zone = "North Shroud",
        string whoName = "Loot Tester",
        string? whoDisplayName = null,
        LootWhoConfidence confidence = LootWhoConfidence.TextOnly,
        uint? itemId = 1001u)
    {
        return new LootRecord
        {
            CapturedAtUtc = new DateTimeOffset(2026, 3, 26, 18, 0, 0, TimeSpan.Zero).AddMinutes(quantity),
            ItemName = itemName,
            Quantity = quantity,
            ZoneName = zone,
            WhoName = whoName,
            WhoDisplayName = whoDisplayName ?? whoName,
            WhoConfidence = confidence,
            ItemId = itemId,
            RawText = $"{whoName} obtain {quantity} {itemName}",
        };
    }
}
