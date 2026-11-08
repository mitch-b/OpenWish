using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using OpenWish.Application.Services;

namespace OpenWish.Web.Services;

public static class GuestEventAuthentication
{
    public const string Scheme = "OpenWish.EventGuest";
    private const string ParticipantClaim = "guest-participant";
    private const string CredentialClaim = "guest-credential";

    public static ClaimsPrincipal CreatePrincipal(GuestEventSession session) =>
        new(new ClaimsIdentity(
            [new Claim(ParticipantClaim, session.ParticipantPublicId), new Claim(CredentialClaim, session.TokenHash)],
            Scheme));

    public static async Task<GuestEventSession?> GetSessionAsync(HttpContext context)
    {
        var result = await context.AuthenticateAsync(Scheme);
        var participant = result.Principal?.FindFirstValue(ParticipantClaim);
        var credential = result.Principal?.FindFirstValue(CredentialClaim);
        return result.Succeeded && participant is not null && credential is not null
            ? new GuestEventSession(participant, credential)
            : null;
    }
}