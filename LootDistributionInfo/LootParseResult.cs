namespace LootDistributionInfo;

public sealed class LootParseResult
{
    public string RawText { get; init; } = string.Empty;

    public string? SubjectText { get; init; }

    public int Quantity { get; init; } = 1;

    public string? ItemName { get; init; }
}
