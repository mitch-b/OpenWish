using System.Globalization;

namespace OpenWish.Shared.Models;

public static class GiftBudget
{
    public const decimal MaximumAmount = 10_000_000m;

    public static bool IsValid(decimal amount) => amount is >= 0 and <= MaximumAmount;

    public static decimal? Parse(string? value) =>
        decimal.TryParse(
            value,
            NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out var amount) && IsValid(amount)
            ? amount
            : null;
}