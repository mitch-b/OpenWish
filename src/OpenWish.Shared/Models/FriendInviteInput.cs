using System.Net.Mail;

namespace OpenWish.Shared.Models;

public sealed record FriendInviteInput(IReadOnlyList<string> Valid, IReadOnlyList<string> Invalid, int Duplicates)
{
    private static readonly char[] _separators = [',', ';', ' ', '\n', '\r', '\t'];

    public static FriendInviteInput Parse(string? text)
    {
        var valid = new List<string>();
        var invalid = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicates = 0;

        foreach (var token in (text ?? string.Empty).Split(_separators, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!IsEmailAddress(token))
            {
                if (!invalid.Contains(token, StringComparer.OrdinalIgnoreCase))
                {
                    invalid.Add(token);
                }
            }
            else if (seen.Add(token))
            {
                valid.Add(token);
            }
            else
            {
                duplicates++;
            }
        }

        return new FriendInviteInput(valid, invalid, duplicates);
    }

    private static bool IsEmailAddress(string value) =>
        value.Length <= 254 &&
        MailAddress.TryCreate(value, out var address) &&
        address.Address == value &&
        address.Host.Contains('.');
}