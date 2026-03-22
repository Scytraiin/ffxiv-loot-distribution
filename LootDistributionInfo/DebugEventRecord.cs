using System;

namespace LootDistributionInfo;

[Serializable]
public sealed class DebugEventRecord
{
    public DateTimeOffset CapturedAtUtc { get; set; }

    public string Area { get; set; } = string.Empty;

    public string Event { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;
}
