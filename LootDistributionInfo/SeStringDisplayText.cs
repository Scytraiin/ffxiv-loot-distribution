using System.Text;

using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;

using Lumina.Excel.Sheets;

namespace LootDistributionInfo;

public static class SeStringDisplayText
{
    public static string Flatten(SeString message)
    {
        var builder = new StringBuilder();

        foreach (var payload in message.Payloads)
        {
            switch (payload)
            {
                case ITextProvider textProvider:
                    builder.Append(textProvider.Text);
                    break;

                case PlayerPayload playerPayload:
                    builder.Append(playerPayload.PlayerName);
                    break;

                case ItemPayload itemPayload:
                    builder.Append(GetItemDisplayName(itemPayload));
                    break;
            }
        }

        return builder.ToString().Trim();
    }

    private static string GetItemDisplayName(ItemPayload itemPayload)
    {
        if (!string.IsNullOrWhiteSpace(itemPayload.DisplayName))
        {
            return itemPayload.DisplayName;
        }

        return itemPayload.Item.GetValueOrDefault<Item>()?.Name.ExtractText()
            ?? itemPayload.Item.GetValueOrDefault<EventItem>()?.Name.ExtractText()
            ?? string.Empty;
    }
}
