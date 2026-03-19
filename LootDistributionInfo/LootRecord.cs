using System;

namespace LootDistributionInfo;

[Serializable]
public sealed class LootRecord
{
    public DateTimeOffset CapturedAtUtc { get; set; }

    public string RawText { get; set; } = string.Empty;

    public string? PlayerName { get; set; }

    public string? ItemText { get; set; }

    public LootCaptureSource Source { get; set; }
}
