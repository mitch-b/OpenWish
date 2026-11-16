using System.Security.Cryptography;
using System.Text;

namespace OpenWish.Shared.Models;

/// <summary>An opaque, stable identifier for a person, so views can join on someone without exposing their account id or relying on display names.</summary>
public static class RecipientKey
{
    public static string For(string userId) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("openwish-recipient:" + userId)))[..24];
}