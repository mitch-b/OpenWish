namespace OpenWish.Shared.Models;

/// <summary>Links a friend's coming birthday to gifts the caller reserved for them and has not bought yet.</summary>
public static class BirthdayGiftNudge
{
    /// <summary>Gifts still to buy per birthday person, keyed by their public display name. People with none are omitted.</summary>
    public static Dictionary<string, int> CountToBuy(GiftPlanModel plan, IEnumerable<UpcomingBirthdayModel> birthdays)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var birthday in birthdays)
        {
            var toBuy = plan.Recipients
                .Where(recipient => string.Equals(recipient.RecipientName, birthday.UserName, StringComparison.OrdinalIgnoreCase))
                .Sum(recipient => recipient.Items.Count(item => !item.IsPurchased));
            if (toBuy > 0)
            {
                counts[birthday.UserName] = toBuy;
            }
        }

        return counts;
    }

    public static string Describe(int toBuy) =>
        toBuy == 1 ? "1 gift still to buy" : $"{toBuy} gifts still to buy";
}
