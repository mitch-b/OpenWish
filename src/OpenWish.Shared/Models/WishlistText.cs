using System.Globalization;
using System.Text;

namespace OpenWish.Shared.Models;

/// <summary>Formats the ideas a viewer can already see as plain text to paste into a message. Reservations and comments are never included.</summary>
public static class WishlistText
{
    public static string Build(string wishlistName, IEnumerable<WishlistItemModel> items, string? wishlistUrl = null)
    {
        var ideas = items
            .Where(item => !item.IsHiddenFromOwner && item.ReceivedOn is null)
            .ToList();

        var builder = new StringBuilder();
        builder.Append(wishlistName).Append('\n');
        foreach (var item in ideas)
        {
            builder.Append("- ").Append(item.Name);
            if (item.Price is { } price)
            {
                builder.Append(" (").Append(FormatPrice(price)).Append(')');
            }

            if (!string.IsNullOrWhiteSpace(item.Url))
            {
                builder.Append(" - ").Append(item.Url.Trim());
            }

            builder.Append('\n');
        }

        if (ideas.Count == 0)
        {
            builder.Append("No ideas yet.\n");
        }

        if (!string.IsNullOrWhiteSpace(wishlistUrl))
        {
            builder.Append('\n').Append(wishlistUrl).Append('\n');
        }

        return builder.ToString();
    }

    private static string FormatPrice(decimal price)
    {
        var text = price.ToString("C", CultureInfo.GetCultureInfo("en-US"));
        return text.EndsWith(".00", StringComparison.Ordinal) ? text[..^3] : text;
    }
}