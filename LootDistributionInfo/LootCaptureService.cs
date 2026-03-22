using System;
using System.Collections.Generic;
using System.Linq;

using Dalamud.Game.Chat;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;

using Lumina.Text.ReadOnly;
using Lumina.Excel.Sheets;

namespace LootDistributionInfo;

public sealed class LootCaptureService : IDisposable
{
    private static readonly TimeSpan DedupeWindow = TimeSpan.FromSeconds(2);

    private readonly Configuration configuration;
    private readonly IChatGui chatGui;
    private readonly IClientState clientState;
    private readonly IDataManager dataManager;
    private readonly IPlayerState playerState;
    private readonly IPartyList partyList;
    private readonly IPluginLog log;
    private readonly LootHistory history;
    private readonly DebugEventBuffer debugEventBuffer;

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
        this.log = log;
        this.history = new LootHistory(this.configuration.RetainHistoryBetweenSessions ? this.configuration.StoredRecords : []);
        this.debugEventBuffer = new DebugEventBuffer();
        this.history.Trim(this.configuration.MaxEntries);

        // The plugin listens to both user-visible chat and raw log messages because some loot lines
        // only surface reliably through one of those hooks depending on how the game formats them.
        this.chatGui.ChatMessage += this.OnChatMessage;
        this.chatGui.LogMessage += this.OnLogMessage;
        this.clientState.TerritoryChanged += this.OnTerritoryChanged;
        this.PersistHistory();
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
        this.Debug("Chat", $"Received {type}: {message.TextValue}");

        if (type is not (XivChatType.Notice or XivChatType.SystemMessage or XivChatType.GatheringSystemMessage))
        {
            this.Debug("Filter", $"Skipped chat type {type}.");
            return;
        }

        this.TryCapture(message.TextValue, LootCaptureSource.ChatMessage);
    }

    private void OnLogMessage(ILogMessage message)
    {
        // v1 intentionally keeps the log path simple and matches the formatted line with the same
        // broad wildcard-style filter used for visible chat text.
        ReadOnlySeString formattedMessage = message.FormatLogMessageForDebugging();
        this.Debug("Log", $"Received log message: {formattedMessage.ExtractText()}");
        this.TryCapture(formattedMessage.ExtractText(), LootCaptureSource.LogMessage);
    }

    private void TryCapture(string rawText, LootCaptureSource source)
    {
        var matchedRecord = LootMatcher.TryMatch(
            rawText,
            source,
            DateTimeOffset.UtcNow,
            this.GetLocalPlayerName(),
            this.GetKnownPartyAndAllianceNames());

        if (matchedRecord is null)
        {
            this.Debug("Matcher", $"Missed {source}: {rawText}");
            return;
        }

        matchedRecord.ZoneName = this.ResolveCurrentZoneName();
        matchedRecord.Normalize();
        this.Debug("Matcher", $"Matched {source}: who={matchedRecord.WhoName ?? "<unknown>"} ({matchedRecord.WhoConfidence}), loot={matchedRecord.LootText ?? "<unknown>"}.");

        // Dedupe keeps the shared chat/log pipeline from storing the same loot line twice when both
        // hooks observe the same event within a small timing window.
        if (!this.history.TryAdd(matchedRecord, this.configuration.MaxEntries, DedupeWindow))
        {
            this.Debug("Dedupe", $"Skipped duplicate line from {source}: {matchedRecord.RawText}");
            return;
        }

        this.log.Verbose("Captured loot line from {Source}: {Text}", source, matchedRecord.RawText);
        this.Debug("History", $"Stored loot event in {matchedRecord.ZoneName}: {matchedRecord.RawText}");
        this.PersistHistory();
    }

    private void PersistHistory()
    {
        this.configuration.StoredRecords = this.configuration.RetainHistoryBetweenSessions
            ? this.history.Snapshot()
            : [];

        // Retention trimming is applied before every save so config size stays bounded even after
        // setting changes or older persisted data is loaded from a previous plugin version.
        this.configuration.Normalize();
        this.configuration.Save();
    }

    private void OnTerritoryChanged(ushort territoryType)
    {
        this.Debug("Zone", $"Entered {this.ResolveZoneName(territoryType)} ({territoryType}).");
    }

    private string ResolveCurrentZoneName()
    {
        return this.ResolveZoneName(this.clientState.TerritoryType);
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

    private IEnumerable<string> GetKnownPartyAndAllianceNames()
    {
        return this.partyList
            .Select(member => member.Name.TextValue)
            .Where(name => !string.IsNullOrWhiteSpace(name));
    }

    private void Debug(string eventName, string details)
    {
        if (!this.configuration.DebugModeEnabled)
        {
            return;
        }

        this.debugEventBuffer.Add(this.ResolveCurrentZoneName(), eventName, details);
    }
}
