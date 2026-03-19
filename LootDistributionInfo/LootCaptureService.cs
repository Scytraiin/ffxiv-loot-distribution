using System;
using System.Collections.Generic;

using Dalamud.Game.Chat;
using Dalamud.Game.Text;
using Dalamud.Plugin.Services;

using Lumina.Text.ReadOnly;

namespace LootDistributionInfo;

public sealed class LootCaptureService : IDisposable
{
    private static readonly TimeSpan DedupeWindow = TimeSpan.FromSeconds(2);

    private readonly Configuration configuration;
    private readonly IChatGui chatGui;
    private readonly IPluginLog log;
    private readonly LootHistory history;

    public LootCaptureService(Configuration configuration, IChatGui chatGui, IPluginLog log)
    {
        this.configuration = configuration;
        this.chatGui = chatGui;
        this.log = log;
        this.history = new LootHistory(this.configuration.RetainHistoryBetweenSessions ? this.configuration.StoredRecords : []);
        this.history.Trim(this.configuration.MaxEntries);

        // The plugin listens to both user-visible chat and raw log messages because some loot lines
        // only surface reliably through one of those hooks depending on how the game formats them.
        this.chatGui.ChatMessage += this.OnChatMessage;
        this.chatGui.LogMessage += this.OnLogMessage;
        this.PersistHistory();
    }

    public IReadOnlyList<LootRecord> Records => this.history.Records;

    public void Dispose()
    {
        // Unsubscribing here keeps plugin reloads from stacking duplicate handlers across sessions.
        this.chatGui.ChatMessage -= this.OnChatMessage;
        this.chatGui.LogMessage -= this.OnLogMessage;
        this.PersistHistory();
    }

    public void ApplyConfigurationChanges()
    {
        this.configuration.Normalize();
        this.history.Trim(this.configuration.MaxEntries);
        this.PersistHistory();
    }

    public void ClearHistory()
    {
        this.history.Clear();
        this.PersistHistory();
    }

    private void OnChatMessage(XivChatType type, int timestamp, ref Dalamud.Game.Text.SeStringHandling.SeString sender, ref Dalamud.Game.Text.SeStringHandling.SeString message, ref bool isHandled)
    {
        if (type is not (XivChatType.Notice or XivChatType.SystemMessage or XivChatType.GatheringSystemMessage))
        {
            return;
        }

        this.TryCapture(message.TextValue, LootCaptureSource.ChatMessage);
    }

    private void OnLogMessage(ILogMessage message)
    {
        // v1 intentionally keeps the log path simple and matches the formatted line with the same
        // broad wildcard-style filter used for visible chat text.
        ReadOnlySeString formattedMessage = message.FormatLogMessageForDebugging();
        this.TryCapture(formattedMessage.ExtractText(), LootCaptureSource.LogMessage);
    }

    private void TryCapture(string rawText, LootCaptureSource source)
    {
        var matchedRecord = LootMatcher.TryMatch(rawText, source, DateTimeOffset.UtcNow);
        if (matchedRecord is null)
        {
            return;
        }

        // Dedupe keeps the shared chat/log pipeline from storing the same loot line twice when both
        // hooks observe the same event within a small timing window.
        if (!this.history.TryAdd(matchedRecord, this.configuration.MaxEntries, DedupeWindow))
        {
            return;
        }

        this.log.Verbose("Captured loot line from {Source}: {Text}", source, matchedRecord.RawText);
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
}
