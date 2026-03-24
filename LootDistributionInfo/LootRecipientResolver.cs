using System;
using System.Collections.Generic;
using System.Linq;

namespace LootDistributionInfo;

public sealed record LootRecipientCandidate(string BaseName, ushort? HomeWorldId, string? WorldName);

public sealed record LootPartyMemberIdentity(string BaseName, string DisplayName, ushort? HomeWorldId, string? WorldName);

public sealed record ResolvedRecipient(
    string? WhoName,
    string? WhoDisplayName,
    string? WhoWorldName,
    ushort? WhoHomeWorldId,
    LootWhoConfidence Confidence);

public static class LootRecipientResolver
{
    public static ResolvedRecipient Resolve(
        string? subjectText,
        IEnumerable<LootRecipientCandidate>? structuredCandidates,
        IEnumerable<LootPartyMemberIdentity> partyMembers,
        string? localPlayerName,
        ushort? localHomeWorldId)
    {
        if (string.IsNullOrWhiteSpace(subjectText))
        {
            return Unknown();
        }

        if (string.Equals(subjectText, "You", StringComparison.OrdinalIgnoreCase))
        {
            var displayName = string.IsNullOrWhiteSpace(localPlayerName) ? "You" : localPlayerName.Trim();
            return new ResolvedRecipient(displayName, displayName, null, localHomeWorldId, LootWhoConfidence.Self);
        }

        var normalizedPartyMembers = partyMembers
            .Select(member => new NormalizedPartyMember(member))
            .Where(member => !string.IsNullOrWhiteSpace(member.BaseName))
            .ToList();

        var orderedCandidates = (structuredCandidates ?? [])
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.BaseName))
            .Select(candidate => new LootRecipientCandidate(candidate.BaseName.Trim(), candidate.HomeWorldId, NormalizeNullable(candidate.WorldName)))
            .OrderByDescending(candidate => CandidateMatchesSubject(subjectText, candidate))
            .ThenByDescending(candidate => candidate.HomeWorldId.HasValue)
            .ToList();

        foreach (var candidate in orderedCandidates)
        {
            var worldVerified = normalizedPartyMembers
                .Where(member => member.BaseKey == LootMatcher.NormalizeForNameMatch(candidate.BaseName)
                    && candidate.HomeWorldId.HasValue
                    && member.HomeWorldId == candidate.HomeWorldId)
                .ToList();
            if (worldVerified.Count == 1)
            {
                return CreateVerified(worldVerified[0], localHomeWorldId);
            }
        }

        foreach (var candidate in orderedCandidates)
        {
            var baseVerified = normalizedPartyMembers
                .Where(member => member.BaseKey == LootMatcher.NormalizeForNameMatch(candidate.BaseName))
                .ToList();
            if (baseVerified.Count == 1)
            {
                return CreateVerified(baseVerified[0], localHomeWorldId);
            }
        }

        var looseSubject = NormalizeLoose(subjectText);
        if (!string.IsNullOrEmpty(looseSubject))
        {
            var worldSuffixMatches = normalizedPartyMembers
                .Where(member => !string.IsNullOrEmpty(member.BaseWithWorldKey) && member.BaseWithWorldKey == looseSubject)
                .ToList();
            if (worldSuffixMatches.Count == 1)
            {
                return CreateVerified(worldSuffixMatches[0], localHomeWorldId);
            }
        }

        if (LootMatcher.LooksLikeTwoWordName(subjectText))
        {
            var baseKey = LootMatcher.NormalizeForNameMatch(subjectText);
            var baseMatches = normalizedPartyMembers.Where(member => member.BaseKey == baseKey).ToList();
            if (baseMatches.Count == 1)
            {
                return CreateVerified(baseMatches[0], localHomeWorldId);
            }

            return CreateTextOnly(subjectText.Trim(), null, null, localHomeWorldId);
        }

        foreach (var candidate in orderedCandidates)
        {
            if (!LootMatcher.LooksLikeTwoWordName(candidate.BaseName))
            {
                continue;
            }

            return CreateTextOnly(candidate.BaseName.Trim(), candidate.WorldName, candidate.HomeWorldId, localHomeWorldId);
        }

        return Unknown();
    }

    private static bool CandidateMatchesSubject(string? subjectText, LootRecipientCandidate candidate)
    {
        if (string.IsNullOrWhiteSpace(subjectText))
        {
            return false;
        }

        var normalizedSubject = LootMatcher.NormalizeForNameMatch(subjectText);
        if (normalizedSubject == LootMatcher.NormalizeForNameMatch(candidate.BaseName))
        {
            return true;
        }

        var looseSubject = NormalizeLoose(subjectText);
        if (looseSubject == NormalizeLoose(candidate.BaseName))
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(candidate.WorldName)
            && looseSubject == NormalizeLoose(candidate.BaseName + candidate.WorldName);
    }

    private static ResolvedRecipient CreateVerified(NormalizedPartyMember member, ushort? localHomeWorldId)
    {
        return new ResolvedRecipient(
            member.BaseName,
            FormatDisplayName(member.BaseName, member.WorldName, member.HomeWorldId, localHomeWorldId),
            member.WorldName,
            member.HomeWorldId,
            LootWhoConfidence.PartyOrAllianceVerified);
    }

    private static ResolvedRecipient CreateTextOnly(string baseName, string? worldName, ushort? homeWorldId, ushort? localHomeWorldId)
    {
        return new ResolvedRecipient(
            baseName,
            FormatDisplayName(baseName, worldName, homeWorldId, localHomeWorldId),
            NormalizeNullable(worldName),
            homeWorldId,
            LootWhoConfidence.TextOnly);
    }

    private static ResolvedRecipient Unknown()
    {
        return new ResolvedRecipient(null, null, null, null, LootWhoConfidence.Unknown);
    }

    private static string FormatDisplayName(string baseName, string? worldName, ushort? worldId, ushort? localHomeWorldId)
    {
        var trimmedBaseName = baseName.Trim();
        if (string.IsNullOrWhiteSpace(trimmedBaseName)
            || string.IsNullOrWhiteSpace(worldName)
            || !worldId.HasValue
            || !localHomeWorldId.HasValue
            || worldId.Value == 0
            || worldId.Value == localHomeWorldId.Value)
        {
            return trimmedBaseName;
        }

        return $"{trimmedBaseName} ({worldName.Trim()})";
    }

    private static string? NormalizeNullable(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string NormalizeLoose(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
    }

    private sealed class NormalizedPartyMember
    {
        public NormalizedPartyMember(LootPartyMemberIdentity member)
        {
            this.BaseName = member.BaseName.Trim();
            this.DisplayName = member.DisplayName.Trim();
            this.HomeWorldId = member.HomeWorldId;
            this.WorldName = NormalizeNullable(member.WorldName);
            this.BaseKey = LootMatcher.NormalizeForNameMatch(this.BaseName);
            this.BaseWithWorldKey = string.IsNullOrWhiteSpace(this.WorldName)
                ? string.Empty
                : NormalizeLoose(this.BaseName + this.WorldName);
        }

        public string BaseName { get; }

        public string DisplayName { get; }

        public ushort? HomeWorldId { get; }

        public string? WorldName { get; }

        public string BaseKey { get; }

        public string BaseWithWorldKey { get; }
    }
}
