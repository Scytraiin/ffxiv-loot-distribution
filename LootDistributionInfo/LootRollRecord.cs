using System;

namespace LootDistributionInfo;

[Serializable]
public sealed class LootRollRecord
{
    public DateTimeOffset CapturedAtUtc { get; set; }

    public string PlayerName { get; set; } = string.Empty;

    public LootRollType RollType { get; set; }

    public int? RollValue { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public string RawText { get; set; } = string.Empty;

    public void Normalize()
    {
        this.PlayerName = this.PlayerName?.Trim() ?? string.Empty;
        this.ItemName = this.ItemName?.Trim() ?? string.Empty;
        this.RawText = this.RawText?.Trim() ?? string.Empty;

        if (this.RollType == LootRollType.Pass)
        {
            this.RollValue = null;
        }
    }

    public string ToSummaryText()
    {
        return this.RollType switch
        {
            LootRollType.Pass => $"{this.PlayerName} Pass",
            _ => $"{this.PlayerName} {this.RollType} {this.RollValue}",
        };
    }
}
