using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using OpenWish.Application.Integrations;
using OpenWish.Application.Models.Configuration;
using OpenWish.Shared.Integrations;
using OpenWish.Web.Integrations;

namespace OpenWish.Web.Controllers;

/// <summary>
/// The API a connected app uses on behalf of the person who connected it.
/// See .docs/INTEGRATIONS.md for the contract.
/// </summary>
[ApiController]
[Route("api/integrations/v1")]
[Authorize(AuthenticationSchemes = IntegrationAuthenticationDefaults.Scheme)]
[EnableRateLimiting(ApiRateLimitPolicy)]
[IgnoreAntiforgeryToken]
public class IntegrationApiController(
    IIntegrationItemService itemService,
    IOptions<OpenWishSettings> settings) : ControllerBase
{
    public const string ApiRateLimitPolicy = "integration-api";

    [HttpGet("me")]
    public async Task<ActionResult<IntegrationProfileModel>> GetProfile()
    {
        var profile = await itemService.GetProfileAsync(Caller);
        foreach (var wishlist in profile.Wishlists)
        {
            wishlist.Url = Absolute(wishlist.Url);
        }

        return Ok(profile);
    }

    [HttpGet("wishlists")]
    public async Task<ActionResult<IEnumerable<IntegrationWishlistModel>>> GetWishlists()
    {
        var wishlists = await itemService.GetWishlistsAsync(Caller);
        foreach (var wishlist in wishlists)
        {
            wishlist.Url = Absolute(wishlist.Url);
        }

        return Ok(wishlists);
    }

    [HttpGet("items")]
    public async Task<ActionResult<IEnumerable<IntegrationItemModel>>> GetItems(
        [FromQuery] string? wishlistId,
        [FromQuery] string? externalId)
    {
        var items = await itemService.GetItemsAsync(Caller, wishlistId, externalId);
        return Ok(items.Select(WithAbsoluteUrl));
    }

    [HttpGet("items/{itemId}", Name = nameof(GetItem))]
    public async Task<ActionResult<IntegrationItemModel>> GetItem(string itemId)
    {
        var item = await itemService.GetItemAsync(Caller, itemId);
        return item is null ? NotFoundError() : Ok(WithAbsoluteUrl(item));
    }

    [HttpPost("items")]
    public async Task<ActionResult<IntegrationItemModel>> CreateItem(IntegrationItemRequest request) =>
        ToActionResult(await itemService.CreateItemAsync(Caller, request));

    [HttpPut("items/{itemId}")]
    public async Task<ActionResult<IntegrationItemModel>> UpdateItem(string itemId, IntegrationItemRequest request) =>
        ToActionResult(await itemService.UpdateItemAsync(Caller, itemId, request));

    [HttpPut("items/external/{externalId}")]
    public async Task<ActionResult<IntegrationItemModel>> UpsertItemByExternalId(string externalId, IntegrationItemRequest request) =>
        ToActionResult(await itemService.UpsertItemByExternalIdAsync(Caller, externalId, request));

    [HttpDelete("items/{itemId}")]
    public async Task<IActionResult> DeleteItem(string itemId) =>
        await itemService.DeleteItemAsync(Caller, itemId) ? NoContent() : NotFoundError();

    [HttpDelete("items/external/{externalId}")]
    public async Task<IActionResult> DeleteItemByExternalId(string externalId) =>
        await itemService.DeleteItemByExternalIdAsync(Caller, externalId) ? NoContent() : NotFoundError();

    private IntegrationCaller Caller => User.GetIntegrationCaller()
        ?? throw new InvalidOperationException("The integration caller was not authenticated.");

    private ActionResult<IntegrationItemModel> ToActionResult(IntegrationWriteResult result) => result.Status switch
    {
        IntegrationWriteStatus.Created => CreatedAtRoute(nameof(GetItem), new { itemId = result.Item!.Id }, WithAbsoluteUrl(result.Item)),
        IntegrationWriteStatus.Updated => Ok(WithAbsoluteUrl(result.Item!)),
        IntegrationWriteStatus.WishlistNotAllowed => StatusCode(StatusCodes.Status403Forbidden, new IntegrationErrorModel
        {
            Error = "wishlist_not_allowed",
            ErrorDescription = "The person has not allowed this app to use that wishlist."
        }),
        IntegrationWriteStatus.DuplicateExternalId => Conflict(new IntegrationErrorModel
        {
            Error = "duplicate_external_id",
            ErrorDescription = "An idea with this externalId already exists. Update it instead.",
            ItemId = result.ExistingItemId
        }),
        _ => NotFoundError()
    };

    private NotFoundObjectResult NotFoundError() =>
        NotFound(new IntegrationErrorModel { Error = "not_found", ErrorDescription = "No idea added by this app matches." });

    private IntegrationItemModel WithAbsoluteUrl(IntegrationItemModel item)
    {
        item.OpenWishUrl = Absolute(item.OpenWishUrl);
        return item;
    }

    private string? Absolute(string? relative) =>
        relative is null ? null : new Uri(PublicBaseUri.Resolve(Request, settings.Value.BaseUri), relative).ToString();
}