namespace LootDistributionInfo;

public sealed class LootParseResult
{
    public string RawText { get; init; } = string.Empty;

    public string? SubjectText { get; init; }

    public string? LootText { get; init; }
}
