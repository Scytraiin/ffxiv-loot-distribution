using Xunit;

namespace LootDistributionInfo.Tests;

public sealed class LootRecipientResolverTests
{
    [Fact]
    public void Resolve_VerifiesCrossWorldPartyMemberFromStructuredCandidate()
    {
        var result = LootRecipientResolver.Resolve(
            "Party FriendSpriggan",
            [new LootRecipientCandidate("Party Friend", 90, "Spriggan")],
            [new LootPartyMemberIdentity("Party Friend", "Party Friend", 90, "Spriggan")],
            "Loot Tester",
            33);

        Assert.Equal("Party Friend", result.WhoName);
        Assert.Equal("Party Friend (Spriggan)", result.WhoDisplayName);
        Assert.Equal("Spriggan", result.WhoWorldName);
        Assert.Equal((ushort)90, result.WhoHomeWorldId);
        Assert.Equal(LootWhoConfidence.PartyOrAllianceVerified, result.Confidence);
    }

    [Fact]
    public void Resolve_VerifiesSameWorldPartyMemberWithoutWorldSuffix()
    {
        var result = LootRecipientResolver.Resolve(
            "Party Friend",
            [],
            [new LootPartyMemberIdentity("Party Friend", "Party Friend", 33, "Twintania")],
            "Loot Tester",
            33);

        Assert.Equal("Party Friend", result.WhoName);
        Assert.Equal("Party Friend", result.WhoDisplayName);
        Assert.Equal(LootWhoConfidence.PartyOrAllianceVerified, result.Confidence);
    }

    [Fact]
    public void Resolve_KeepsTextOnlyForNonPartyRecipient()
    {
        var result = LootRecipientResolver.Resolve(
            "Other Person",
            [],
            [new LootPartyMemberIdentity("Party Friend", "Party Friend", 33, "Twintania")],
            "Loot Tester",
            33);

        Assert.Equal("Other Person", result.WhoName);
        Assert.Equal("Other Person", result.WhoDisplayName);
        Assert.Equal(LootWhoConfidence.TextOnly, result.Confidence);
    }

    [Fact]
    public void Resolve_DoesNotGuessAmbiguousSameNamePartyMembersWithoutWorld()
    {
        var result = LootRecipientResolver.Resolve(
            "Shared Name",
            [],
            [
                new LootPartyMemberIdentity("Shared Name", "Shared Name", 40, "Phoenix"),
                new LootPartyMemberIdentity("Shared Name", "Shared Name", 90, "Spriggan"),
            ],
            "Loot Tester",
            33);

        Assert.Equal("Shared Name", result.WhoName);
        Assert.Equal("Shared Name", result.WhoDisplayName);
        Assert.Equal(LootWhoConfidence.TextOnly, result.Confidence);
    }

    [Fact]
    public void Resolve_UsesLocalPlayerForYou()
    {
        var result = LootRecipientResolver.Resolve(
            "You",
            [],
            [],
            "Loot Tester",
            33);

        Assert.Equal("Loot Tester", result.WhoName);
        Assert.Equal("Loot Tester", result.WhoDisplayName);
        Assert.Equal(LootWhoConfidence.Self, result.Confidence);
    }
}
