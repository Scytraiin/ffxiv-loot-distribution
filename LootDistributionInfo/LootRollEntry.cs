using System;

namespace LootDistributionInfo;

[Serializable]
public sealed class LootRollEntry
{
    public DateTimeOffset CapturedAtUtc { get; set; }

    public string PlayerName { get; set; } = string.Empty;

    public string PlayerDisplayName { get; set; } = string.Empty;

    public string? PlayerWorldName { get; set; }

    public ushort? PlayerHomeWorldId { get; set; }

    public LootRollType RollType { get; set; }

    public int? RollValue { get; set; }

    public string ItemKey { get; set; } = string.Empty;

    public string ItemName { get; set; } = string.Empty;

    public void Normalize()
    {
        this.PlayerName = (this.PlayerName ?? string.Empty).Trim();
        this.PlayerDisplayName = string.IsNullOrWhiteSpace(this.PlayerDisplayName)
            ? this.PlayerName
            : this.PlayerDisplayName.Trim();
        this.PlayerWorldName = NormalizeNullable(this.PlayerWorldName);
        this.ItemKey = (this.ItemKey ?? string.Empty).Trim();
        this.ItemName = (this.ItemName ?? string.Empty).Trim();

        if (this.RollType == LootRollType.Pass)
        {
            this.RollValue = null;
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
