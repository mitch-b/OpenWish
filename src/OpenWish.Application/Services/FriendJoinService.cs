using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenWish.Application.Models.Configuration;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;
using QRCoder;

namespace OpenWish.Application.Services;

public class FriendJoinService(IDbContextFactory<ApplicationDbContext> contextFactory,
    INotificationService notificationService,
    IOptions<OpenWishSettings> openWishSettings) : IFriendJoinService
{
    public const int CodeLength = 8;
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(15);

    // No 0/O, 1/I/L or other look-alikes, so a code can be read aloud or typed from a screen.
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory = contextFactory;
    private readonly INotificationService _notificationService = notificationService;
    private readonly string? _baseUri = openWishSettings.Value.BaseUri;

    public async Task<FriendJoinCodeModel> CreateJoinCodeAsync(string userId, string? origin = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));

        await using var context = await _contextFactory.CreateDbContextAsync();
        if (!await context.Users.AnyAsync(u => u.Id == userId))
        {
            throw new KeyNotFoundException("The requested user could not be found.");
        }

        var now = DateTimeOffset.UtcNow;
        var previous = await context.FriendJoinCodes
            .Where(c => c.UserId == userId && !c.Deleted)
            .ToListAsync();
        foreach (var old in previous)
        {
            old.Deleted = true;
            old.UpdatedOn = now;
        }

        string code;
        string hash;
        do
        {
            code = GenerateCode();
            hash = Hash(code);
        }
        while (await context.FriendJoinCodes.AnyAsync(c => c.CodeHash == hash && !c.Deleted));

        var entity = new FriendJoinCode
        {
            UserId = userId,
            CodeHash = hash,
            ExpiresOn = now + CodeLifetime,
            CreatedOn = now,
            UpdatedOn = now
        };
        context.FriendJoinCodes.Add(entity);
        await context.SaveChangesAsync();

        var joinUrl = $"{ResolveOrigin(origin)}/friends/join/{code}";
        return new FriendJoinCodeModel
        {
            Code = code,
            JoinUrl = joinUrl,
            ExpiresOn = entity.ExpiresOn,
            QrSvg = RenderQrSvg(joinUrl)
        };
    }

    public async Task<bool> CancelJoinCodeAsync(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));

        await using var context = await _contextFactory.CreateDbContextAsync();
        var active = await context.FriendJoinCodes
            .Where(c => c.UserId == userId && !c.Deleted)
            .ToListAsync();
        foreach (var code in active)
        {
            code.Deleted = true;
            code.UpdatedOn = DateTimeOffset.UtcNow;
        }

        await context.SaveChangesAsync();
        return active.Count > 0;
    }

    public async Task<FriendJoinPreviewModel> PreviewJoinCodeAsync(string userId, string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));

        await using var context = await _contextFactory.CreateDbContextAsync();
        var (_, preview) = await ResolveAsync(context, userId, code);
        return preview;
    }

    public async Task<FriendJoinPreviewModel> RedeemJoinCodeAsync(string userId, string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId, nameof(userId));

        await using var context = await _contextFactory.CreateDbContextAsync();
        var (entity, preview) = await ResolveAsync(context, userId, code);
        if (entity is null || preview.Status != FriendJoinStatus.Valid)
        {
            return preview;
        }

        var ownerId = entity.UserId;
        var now = DateTimeOffset.UtcNow;

        // Same reactivate-or-create handling as accepting a friend request, so soft-deleted rows are reused.
        foreach (var (from, to) in new[] { (userId, ownerId), (ownerId, userId) })
        {
            var existing = await context.Friends.FirstOrDefaultAsync(f => f.UserId == from && f.FriendUserId == to);
            if (existing is not null)
            {
                existing.Deleted = false;
                existing.UpdatedOn = now;
                existing.FriendshipDate = now;
            }
            else
            {
                context.Friends.Add(new Friend
                {
                    UserId = from,
                    FriendUserId = to,
                    FriendshipDate = now,
                    CreatedOn = now,
                    UpdatedOn = now
                });
            }
        }

        // Any pending request between them is now moot.
        var pending = await context.FriendRequests
            .Where(r => !r.Deleted && r.Status == "Pending" &&
                ((r.RequesterId == userId && r.ReceiverId == ownerId) ||
                 (r.RequesterId == ownerId && r.ReceiverId == userId)))
            .ToListAsync();
        foreach (var request in pending)
        {
            request.Status = "Accepted";
            request.UpdatedOn = now;
        }

        entity.UsedOn = now;
        entity.Deleted = true;
        entity.UpdatedOn = now;

        var redeemerName = PublicDisplayName.From(
            await context.Users.Where(u => u.Id == userId).Select(u => u.UserName).FirstOrDefaultAsync(),
            "Someone");

        await context.SaveChangesAsync();

        try
        {
            await _notificationService.CreateNotificationAsync(ownerId, $"{redeemerName} connected with you in person.");
        }
        catch
        {
            // The friendship is already saved; a missed notification must not undo it.
        }

        return new FriendJoinPreviewModel { Status = FriendJoinStatus.Connected, DisplayName = preview.DisplayName };
    }

    private static async Task<(FriendJoinCode? Entity, FriendJoinPreviewModel Preview)> ResolveAsync(
        ApplicationDbContext context, string userId, string? rawCode)
    {
        var invalid = new FriendJoinPreviewModel { Status = FriendJoinStatus.Invalid };
        var normalized = Normalize(rawCode);
        if (normalized is null)
        {
            return (null, invalid);
        }

        var hash = Hash(normalized);
        var now = DateTimeOffset.UtcNow;
        var entity = await context.FriendJoinCodes
            .FirstOrDefaultAsync(c => c.CodeHash == hash && !c.Deleted && c.UsedOn == null && c.ExpiresOn > now);
        if (entity is null)
        {
            return (null, invalid);
        }

        var ownerName = await context.Users
            .Where(u => u.Id == entity.UserId)
            .Select(u => u.UserName)
            .FirstOrDefaultAsync();
        if (ownerName is null && !await context.Users.AnyAsync(u => u.Id == entity.UserId))
        {
            return (null, invalid);
        }

        var displayName = PublicDisplayName.From(ownerName, "OpenWish member");
        if (entity.UserId == userId)
        {
            return (entity, new FriendJoinPreviewModel { Status = FriendJoinStatus.OwnCode });
        }

        var alreadyFriends = await context.Friends
            .AnyAsync(f => !f.Deleted && f.UserId == userId && f.FriendUserId == entity.UserId);
        return (entity, new FriendJoinPreviewModel
        {
            Status = alreadyFriends ? FriendJoinStatus.AlreadyFriends : FriendJoinStatus.Valid,
            DisplayName = displayName
        });
    }

    private static string GenerateCode()
    {
        Span<char> chars = stackalloc char[CodeLength];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return new string(chars);
    }

    /// <summary>Uppercases and strips spaces and dashes; returns null if the result cannot be a code.</summary>
    internal static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var cleaned = new string(raw.Where(ch => ch is not (' ' or '-')).Select(char.ToUpperInvariant).ToArray());
        return cleaned.Length == CodeLength && cleaned.All(ch => Alphabet.Contains(ch)) ? cleaned : null;
    }

    private static string Hash(string code) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));

    private string ResolveOrigin(string? origin)
    {
        var value = string.IsNullOrWhiteSpace(origin) ? _baseUri : origin;
        return (value ?? string.Empty).TrimEnd('/');
    }

    private static string RenderQrSvg(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
        return new SvgQRCode(data).GetGraphic(new System.Drawing.Size(240, 240), "#000000", "#ffffff", drawQuietZones: true, SvgQRCode.SizingMode.ViewBoxAttribute);
    }
}