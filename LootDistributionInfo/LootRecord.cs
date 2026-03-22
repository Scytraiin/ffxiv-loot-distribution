using System;

namespace LootDistributionInfo;

[Serializable]
public enum LootWhoConfidence
{
    Unknown = 0,
    Self = 1,
    PartyOrAllianceVerified = 2,
    TextOnly = 3,
}

[Serializable]
public sealed class LootRecord
{
    public DateTimeOffset CapturedAtUtc { get; set; }

    public string ZoneName { get; set; } = string.Empty;

    public string RawText { get; set; } = string.Empty;

    public string? WhoName { get; set; }

    public string? LootText { get; set; }

    public LootWhoConfidence WhoConfidence { get; set; }

    // These compatibility properties preserve older saved configs that stored PlayerName/ItemText.
    public string? PlayerName
    {
        get => this.WhoName;
        set
        {
            if (string.IsNullOrWhiteSpace(this.WhoName))
            {
                this.WhoName = value;
            }
        }
    }

    public string? ItemText
    {
        get => this.LootText;
        set
        {
            if (string.IsNullOrWhiteSpace(this.LootText))
            {
                this.LootText = value;
            }
        }
    }

    public LootCaptureSource Source { get; set; }

    public void Normalize()
    {
        this.ZoneName = this.ZoneName?.Trim() ?? string.Empty;
        this.RawText = this.RawText?.Trim() ?? string.Empty;
        this.WhoName = NormalizeNullable(this.WhoName);
        this.LootText = NormalizeNullable(this.LootText);

        if (this.WhoName is null && !string.IsNullOrWhiteSpace(this.PlayerName))
        {
            this.WhoName = NormalizeNullable(this.PlayerName);
        }

        if (this.LootText is null && !string.IsNullOrWhiteSpace(this.ItemText))
        {
            this.LootText = NormalizeNullable(this.ItemText);
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
