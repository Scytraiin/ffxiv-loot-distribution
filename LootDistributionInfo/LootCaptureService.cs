using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.Chat;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;

using Lumina.Text.ReadOnly;
using Lumina.Excel.Sheets;

namespace LootDistributionInfo;

public sealed class LootCaptureService : IDisposable
{
    private static readonly TimeSpan DedupeWindow = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan RollCorrelationWindow = TimeSpan.FromMinutes(5);

    private readonly Configuration configuration;
    private readonly IChatGui chatGui;
    private readonly IClientState clientState;
    private readonly IDataManager dataManager;
    private readonly IPlayerState playerState;
    private readonly IPartyList partyList;
    private readonly LootHistory history;
    private readonly DebugEventBuffer debugEventBuffer;
    private readonly PendingRollTracker pendingRollTracker;
    private string currentZoneName;

    public LootCaptureService(
        Configuration configuration,
        IChatGui chatGui,
        IClientState clientState,
        IDataManager dataManager,
        IPlayerState playerState,
        IPartyList partyList,
        IPluginLog log)
    {
        this.configuration = configuration;
        this.chatGui = chatGui;
        this.clientState = clientState;
        this.dataManager = dataManager;
        this.playerState = playerState;
        this.partyList = partyList;
        this.history = new LootHistory(this.configuration.RetainHistoryBetweenSessions ? this.configuration.StoredRecords : []);
        this.debugEventBuffer = new DebugEventBuffer();
        this.pendingRollTracker = new PendingRollTracker();
        this.currentZoneName = this.ResolveZoneName(this.clientState.TerritoryType);
        this.history.Trim(this.configuration.MaxEntries);

        // The plugin listens to both user-visible chat and raw log messages because some loot lines
        // only surface reliably through one of those hooks depending on how the game formats them.
        this.chatGui.ChatMessage += this.OnChatMessage;
        this.chatGui.LogMessage += this.OnLogMessage;
        this.clientState.TerritoryChanged += this.OnTerritoryChanged;
        this.PersistHistory();
        log.Information("Loot Distribution Info initialized.");
        this.Debug("Startup", "Plugin initialized.");
    }

    public IReadOnlyList<LootRecord> Records => this.history.Records;

    public IReadOnlyList<DebugEventRecord> DebugEvents => this.debugEventBuffer.Records;

    public bool DebugModeEnabled => this.configuration.DebugModeEnabled;

    public void Dispose()
    {
        this.Debug("Shutdown", "Plugin is shutting down.");

        // Unsubscribing here keeps plugin reloads from stacking duplicate handlers across sessions.
        this.chatGui.ChatMessage -= this.OnChatMessage;
        this.chatGui.LogMessage -= this.OnLogMessage;
        this.clientState.TerritoryChanged -= this.OnTerritoryChanged;
        this.PersistHistory();
    }

    public void ApplyConfigurationChanges()
    {
        this.configuration.Normalize();
        this.history.Trim(this.configuration.MaxEntries);
        this.PersistHistory();

        if (this.configuration.DebugModeEnabled)
        {
            this.Debug("Settings", $"Debug tools {(this.configuration.DebugModeEnabled ? "enabled" : "disabled")}.");
        }
    }

    public void ClearHistory()
    {
        this.history.Clear();
        this.PersistHistory();
        this.Debug("History", "Loot history cleared.");
    }

    public void ClearDebugEvents()
    {
        this.debugEventBuffer.Clear();
    }

    private void OnChatMessage(XivChatType type, int timestamp, ref Dalamud.Game.Text.SeStringHandling.SeString sender, ref Dalamud.Game.Text.SeStringHandling.SeString message, ref bool isHandled)
    {
        var flattenedMessage = this.FlattenMessageText(message);
        this.Debug("Chat", $"Received {type}: {flattenedMessage}");

        if (type is not (XivChatType.Notice or XivChatType.SystemMessage or XivChatType.GatheringSystemMessage))
        {
            this.Debug("Filter", $"Skipped chat type {type}.");
            return;
        }

        this.ProcessIncomingText(flattenedMessage, LootCaptureSource.ChatMessage);
    }

    private void OnLogMessage(ILogMessage message)
    {
        // v1 intentionally keeps the log path simple and matches the formatted line with the same
        // broad wildcard-style filter used for visible chat text.
        ReadOnlySeString formattedMessage = message.FormatLogMessageForDebugging();
        this.Debug("Log", $"Received log message: {formattedMessage.ExtractText()}");
        this.ProcessIncomingText(formattedMessage.ExtractText(), LootCaptureSource.LogMessage);
    }

    private void ProcessIncomingText(string rawText, LootCaptureSource source)
    {
        this.ExpirePendingRollSessions(DateTimeOffset.UtcNow);

        if (this.TryCaptureRoll(rawText, source))
        {
            return;
        }

        this.TryCaptureLoot(rawText, source);
    }

    private bool TryCaptureRoll(string rawText, LootCaptureSource source)
    {
        var parsedRoll = LootRollMatcher.TryMatch(rawText);
        if (parsedRoll is null)
        {
            this.Debug("Roll parser", $"Missed {source}: {rawText}");
            return false;
        }

        parsedRoll.CapturedAtUtc = DateTimeOffset.UtcNow;
        parsedRoll.PlayerName = this.ResolveRollPlayerName(parsedRoll.PlayerName);
        parsedRoll.Normalize();

        var result = this.pendingRollTracker.AddRoll(parsedRoll, this.currentZoneName, RollCorrelationWindow, DedupeWindow);
        switch (result)
        {
            case PendingRollAddResult.Duplicate:
                this.Debug("Roll dedupe", $"Skipped duplicate roll from {source}: {parsedRoll.RawText}");
                break;

            case PendingRollAddResult.CreatedSession:
                this.Debug("Roll session", $"Created session for {parsedRoll.ItemName} in {this.currentZoneName}.");
                this.Debug("Roll parser", $"Matched {source}: {parsedRoll.ToSummaryText()} on {parsedRoll.ItemName}.");
                break;

            case PendingRollAddResult.AddedToExistingSession:
                this.Debug("Roll session", $"Added roll to session for {parsedRoll.ItemName}: {parsedRoll.ToSummaryText()}.");
                this.Debug("Roll parser", $"Matched {source}: {parsedRoll.ToSummaryText()} on {parsedRoll.ItemName}.");
                break;
        }

        return true;
    }

    private void TryCaptureLoot(string rawText, LootCaptureSource source)
    {
        var parsedLoot = LootMatcher.TryMatch(rawText);
        if (parsedLoot is null)
        {
            this.Debug("Matcher", $"Missed {source}: {rawText}");
            return;
        }

        var matchedRecord = this.BuildRecord(parsedLoot, source);
        this.Debug("Matcher", $"Matched {source}: who={matchedRecord.WhoName ?? "<unknown>"} ({matchedRecord.WhoConfidence}), loot={matchedRecord.LootText ?? "<unknown>"}.");

        var matchedRollSession = this.pendingRollTracker.TryResolve(matchedRecord.LootText ?? matchedRecord.RawText, matchedRecord.ZoneName, matchedRecord.CapturedAtUtc, RollCorrelationWindow);
        if (matchedRollSession is not null)
        {
            matchedRecord.AttachRolls(matchedRollSession.Entries);
            this.Debug("Roll correlate", $"Attached {matchedRollSession.Entries.Count} roll(s) to {matchedRecord.LootText ?? matchedRecord.RawText}.");
        }
        else
        {
            this.Debug("Roll correlate", $"No rolls matched for {matchedRecord.LootText ?? matchedRecord.RawText}.");
        }

        // Dedupe keeps the shared chat/log pipeline from storing the same loot line twice when both
        // hooks observe the same event within a small timing window.
        if (!this.history.TryAdd(matchedRecord, this.configuration.MaxEntries, DedupeWindow))
        {
            this.Debug("Dedupe", $"Skipped duplicate line from {source}: {matchedRecord.RawText}");
            return;
        }

        this.Debug("History", $"Stored loot event in {matchedRecord.ZoneName}: {matchedRecord.RawText}");
        this.PersistHistory();
    }

    private void PersistHistory()
    {
        this.configuration.StoredRecords = this.configuration.RetainHistoryBetweenSessions
            ? this.history.Snapshot()
            : [];

        this.configuration.Save();
    }

    private void OnTerritoryChanged(ushort territoryType)
    {
        this.currentZoneName = this.ResolveZoneName(territoryType);
        this.Debug("Zone", $"Entered {this.currentZoneName} ({territoryType}).");
    }

    private string ResolveZoneName(ushort territoryType)
    {
        if (territoryType == 0)
        {
            return "Unknown";
        }

        if (!this.dataManager.GetExcelSheet<TerritoryType>().TryGetRow(territoryType, out var territory))
        {
            return $"Territory {territoryType}";
        }

        var zoneName = territory.PlaceName.ValueNullable?.Name.ExtractText();
        return string.IsNullOrWhiteSpace(zoneName) ? $"Territory {territoryType}" : zoneName;
    }

    private string? GetLocalPlayerName()
    {
        return this.playerState.IsLoaded && !string.IsNullOrWhiteSpace(this.playerState.CharacterName)
            ? this.playerState.CharacterName
            : null;
    }

    private HashSet<string> GetKnownPartyAndAllianceNames()
    {
        return this.partyList
            .Select(member => member.Name.TextValue)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(LootMatcher.NormalizeForNameMatch)
            .ToHashSet(StringComparer.Ordinal);
    }

    private LootRecord BuildRecord(LootParseResult parsedLoot, LootCaptureSource source)
    {
        var (whoName, confidence) = this.ResolveWho(parsedLoot.SubjectText);

        var record = new LootRecord
        {
            CapturedAtUtc = DateTimeOffset.UtcNow,
            ZoneName = this.currentZoneName,
            RawText = parsedLoot.RawText,
            WhoName = whoName,
            LootText = parsedLoot.LootText,
            WhoConfidence = confidence,
            Source = source,
        };

        record.Normalize();
        return record;
    }

    private string ResolveRollPlayerName(string playerName)
    {
        return string.Equals(playerName, "You", StringComparison.OrdinalIgnoreCase)
            ? this.GetLocalPlayerName() ?? "You"
            : playerName.Trim();
    }

    private (string? WhoName, LootWhoConfidence Confidence) ResolveWho(string? subjectText)
    {
        if (string.IsNullOrWhiteSpace(subjectText))
        {
            return (null, LootWhoConfidence.Unknown);
        }

        if (string.Equals(subjectText, "You", StringComparison.OrdinalIgnoreCase))
        {
            return (this.GetLocalPlayerName() ?? "You", LootWhoConfidence.Self);
        }

        if (!LootMatcher.LooksLikeTwoWordName(subjectText))
        {
            return (null, LootWhoConfidence.Unknown);
        }

        var normalizedCandidate = LootMatcher.NormalizeForNameMatch(subjectText);
        var confidence = this.GetKnownPartyAndAllianceNames().Contains(normalizedCandidate)
            ? LootWhoConfidence.PartyOrAllianceVerified
            : LootWhoConfidence.TextOnly;

        return (subjectText, confidence);
    }

    private void Debug(string eventName, string details)
    {
        if (!this.configuration.DebugModeEnabled)
        {
            return;
        }

        this.debugEventBuffer.Add(this.currentZoneName, eventName, details);
    }

    private string FlattenMessageText(SeString message)
    {
        var flattened = SeStringDisplayText.Flatten(message);
        return string.IsNullOrWhiteSpace(flattened) ? message.TextValue.Trim() : flattened;
    }

    private void ExpirePendingRollSessions(DateTimeOffset now)
    {
        foreach (var expiredSession in this.pendingRollTracker.Expire(now, RollCorrelationWindow))
        {
            this.Debug("Roll expire", $"Expired unresolved rolls for {expiredSession.NormalizedItemName} in {expiredSession.ZoneName}.");
        }
    }
}
