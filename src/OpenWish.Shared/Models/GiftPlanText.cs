using System.Globalization;
using System.Text;

namespace OpenWish.Shared.Models;

/// <summary>Formats the caller's still-to-buy gifts as a plain-text shopping list to take to the store.</summary>
public static class GiftPlanText
{
    public static string Build(GiftPlanModel plan, DateTimeOffset now)
    {
        var builder = new StringBuilder("Gift shopping list\n");
        var recipients = plan.GetRecipientsToBuy(now);
        if (recipients.Count == 0)
        {
            return builder.Append("Nothing left to buy.\n").ToString();
        }

        foreach (var recipient in recipients)
        {
            AppendRecipient(builder.Append('\n'), recipient, now);
        }

        builder.Append("\nEstimated to buy: ").Append(FormatPrice(plan.ToBuyTotal));
        if (recipients.Sum(recipient => recipient.UnpricedCount) is var unpriced and > 0)
        {
            builder.Append(" (").Append(unpriced == 1 ? "1 gift has no price" : $"{unpriced} gifts have no price").Append(')');
        }

        return builder.Append('\n').ToString();
    }

    /// <summary>Formats one person's still-to-buy gifts, for shopping for them on their own.</summary>
    public static string BuildForRecipient(GiftPlanRecipientModel recipient, DateTimeOffset now)
    {
        var remaining = recipient with { Items = recipient.Items.Where(item => !item.IsPurchased).ToList() };
        var builder = new StringBuilder("Gifts for ");
        if (remaining.Items.Count == 0)
        {
            return builder.Append(recipient.RecipientName).Append("\nNothing left to buy.\n").ToString();
        }

        AppendRecipient(builder, remaining, now);
        builder.Append("\nEstimated to buy: ").Append(FormatPrice(remaining.Total));
        if (remaining.UnpricedCount > 0)
        {
            builder.Append(" (").Append(remaining.UnpricedCount == 1 ? "1 gift has no price" : $"{remaining.UnpricedCount} gifts have no price").Append(')');
        }

        return builder.Append('\n').ToString();
    }

    private static void AppendRecipient(StringBuilder builder, GiftPlanRecipientModel recipient, DateTimeOffset now)
    {
        builder.Append(recipient.RecipientName);
        if (recipient.GetNextOccasion(now) is { } occasion && !EventCountdown.IsPast(occasion, now))
        {
            builder.Append(" (").Append(EventCountdown.Badge(occasion, now).ToLowerInvariant()).Append(')');
        }

        builder.Append('\n');
        foreach (var item in recipient.Items)
        {
            builder.Append("- ").Append(item.ItemName);
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
    }

    // Matches the prices on the page, which use the current culture.
    private static string FormatPrice(decimal price) => price.ToString("C", CultureInfo.CurrentCulture);
}