using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using OpenWish.Application.Integrations;
using OpenWish.Application.Models.Configuration;
using OpenWish.Shared.Integrations;

namespace OpenWish.Web.Integrations;

/// <summary>
/// The tools AI assistants see at <c>/mcp</c>. They offer exactly what the person approved on the
/// consent screen: allowed lists and, optionally, existing ideas. Reservations, purchases,
/// comments, friends, and hidden surprises are never exposed.
/// </summary>
[McpServerToolType]
public sealed class OpenWishMcpTools(
    IIntegrationItemService itemService,
    IHttpContextAccessor httpContextAccessor,
    IOptions<OpenWishSettings> settings)
{
    public const string ServerInstructions =
        "OpenWish keeps a person's gift wishlists. Use these tools to save gift ideas the person asks you to remember. " +
        "You can only use allowed lists. When managing existing items is enabled, you can also see and manage ideas the person or other apps added. " +
        "Otherwise, only ideas you added are accessible. You never see reservations, purchases, comments, or hidden surprises.";

    [McpServerTool(Name = "list_wishlists", Title = "List wishlists", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Lists the wishlists the person allowed this assistant to add ideas to. The default list is where new ideas go when no list is named.")]
    public async Task<IReadOnlyList<IntegrationWishlistModel>> ListWishlists()
    {
        var wishlists = await itemService.GetWishlistsAsync(Caller);
        foreach (var wishlist in wishlists)
        {
            wishlist.Url = Absolute(wishlist.Url);
        }

        return wishlists;
    }

    [McpServerTool(Name = "list_ideas", Title = "List allowed ideas", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("Lists ideas in allowed wishlists. Includes existing ideas when the person allows managing them; otherwise shows only ideas this assistant added.")]
    public async Task<IReadOnlyList<IntegrationItemModel>> ListIdeas(
        [Description("Only list ideas on this wishlist ID. Omit to list ideas on every allowed list.")] string? wishlistId = null)
    {
        var items = await itemService.GetItemsAsync(Caller, wishlistId, externalId: null);
        return items.Select(WithAbsoluteUrl).ToList();
    }

    [McpServerTool(Name = "add_idea", Title = "Add a gift idea", OpenWorld = false)]
    [Description("Adds a gift idea to one of the person's allowed wishlists.")]
    public async Task<IntegrationItemModel> AddIdea(
        [Description("A short name for the idea, such as \"Cast-iron skillet\".")] string name,
        [Description("Notes such as size, color, or why it's a good fit.")] string? description = null,
        [Description("A product page, as an absolute http or https URL.")] string? url = null,
        [Description("An image of the idea, as an absolute http or https URL.")] string? imageUrl = null,
        [Description("The approximate price, with at most two decimal places.")] decimal? price = null,
        [Description("Where to buy it, such as a store name.")] string? whereToBuy = null,
        [Description("1 high, 2 medium, or 3 low.")] int? priority = null,
        [Description("The wishlist ID to add to. Omit to use the default list.")] string? wishlistId = null)
    {
        var request = new IntegrationItemRequest
        {
            WishlistId = wishlistId,
            Name = name,
            Description = description,
            Url = url,
            ImageUrl = imageUrl,
            Price = price,
            WhereToBuy = whereToBuy,
            Priority = priority
        };
        Validate(request);
        return Unwrap(await itemService.CreateItemAsync(Caller, request));
    }

    [McpServerTool(Name = "update_idea", Title = "Update an allowed idea", Idempotent = true, OpenWorld = false)]
    [Description("Changes an accessible idea in an allowed list, including existing ideas when permitted. Only the fields you pass change; pass an empty string to clear a text field.")]
    public async Task<IntegrationItemModel> UpdateIdea(
        [Description("The idea ID from add_idea or list_ideas.")] string ideaId,
        [Description("A new name.")] string? name = null,
        [Description("New notes, or an empty string to clear them.")] string? description = null,
        [Description("A new product URL, or an empty string to clear it.")] string? url = null,
        [Description("A new image URL, or an empty string to clear it.")] string? imageUrl = null,
        [Description("A new price.")] decimal? price = null,
        [Description("Where to buy it, or an empty string to clear it.")] string? whereToBuy = null,
        [Description("1 high, 2 medium, 3 low, or 0 to clear.")] int? priority = null,
        [Description("Move the idea to this allowed wishlist ID.")] string? wishlistId = null)
    {
        var caller = Caller;
        var existing = await itemService.GetItemAsync(caller, ideaId)
            ?? throw new McpException("No accessible idea has that ID. Call list_ideas to see allowed ideas.");

        var request = new IntegrationItemRequest
        {
            WishlistId = wishlistId,
            Name = name,
            Description = description,
            Url = url,
            ImageUrl = imageUrl,
            Price = price,
            WhereToBuy = whereToBuy,
            Priority = priority
        };
        Validate(request, partial: true);
        request.WishlistId ??= existing.WishlistId;
        request.Name ??= existing.Name;
        request.Description ??= existing.Description;
        request.Url ??= existing.Url;
        request.ImageUrl ??= existing.ImageUrl;
        request.Price ??= existing.Price;
        request.WhereToBuy ??= existing.WhereToBuy;
        return Unwrap(await itemService.UpdateItemAsync(caller, ideaId, request));
    }

    [McpServerTool(Name = "remove_idea", Title = "Remove an allowed idea", Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("Removes an accessible idea in an allowed list, including existing ideas when the person permits managing them.")]
    public async Task<string> RemoveIdea([Description("The idea ID from add_idea or list_ideas.")] string ideaId) =>
        await itemService.DeleteItemAsync(Caller, ideaId)
            ? "Removed the idea."
            : throw new McpException("No accessible idea has that ID. Call list_ideas to see allowed ideas.");

    private IntegrationCaller Caller =>
        httpContextAccessor.HttpContext?.User.GetIntegrationCaller()
            ?? throw new McpException("This connection is no longer authorized. Connect OpenWish again.");

    private static void Validate(IntegrationItemRequest request, bool partial = false)
    {
        var results = new List<ValidationResult>();
        if (partial)
        {
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(request))
            {
                var value = property.GetValue(request);
                if (value is not null)
                {
                    Validator.TryValidateProperty(value,
                        new ValidationContext(request) { MemberName = property.Name }, results);
                }
            }
            results.AddRange(request.Validate(new ValidationContext(request))
                .Where(error => request.Name is not null || !error.MemberNames.Contains(nameof(IntegrationItemRequest.Name))));
        }
        else
        {
            Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        }

        if (results.Count > 0)
        {
            throw new McpException(string.Join(" ", results.Select(r => r.ErrorMessage).Distinct()));
        }
    }

    private IntegrationItemModel Unwrap(IntegrationWriteResult result) => result.Status switch
    {
        IntegrationWriteStatus.Created or IntegrationWriteStatus.Updated when result.Item is not null => WithAbsoluteUrl(result.Item),
        IntegrationWriteStatus.WishlistNotAllowed => throw new McpException(
            "That wishlist isn't one the person allowed. Call list_wishlists to see the allowed lists."),
        _ => throw new McpException("No accessible idea has that ID. Call list_ideas to see allowed ideas.")
    };

    private IntegrationItemModel WithAbsoluteUrl(IntegrationItemModel item)
    {
        item.OpenWishUrl = Absolute(item.OpenWishUrl);
        return item;
    }

    private string? Absolute(string? relative)
    {
        var request = httpContextAccessor.HttpContext?.Request;
        return relative is null || request is null
            ? relative
            : new Uri(PublicBaseUri.Resolve(request, settings.Value.BaseUri), relative).ToString();
    }
}