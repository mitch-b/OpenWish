using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using OpenWish.Application.Models;
using OpenWish.Data.Entities;
using OpenWish.Shared.Models;
using Xunit;

namespace OpenWish.Application.Tests.Models;

public class OpenWishProfileSecurityTests
{
    private readonly IMapper _mapper = new MapperConfiguration(
        configuration => configuration.AddProfile<OpenWishProfile>(),
        NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public void MapWishlistUpdate_PreservesServerOwnedFields()
    {
        var entity = new Wishlist
        {
            PublicId = "server-public-id",
            Name = "Original name",
            OwnerId = "owner-id",
            Deleted = false
        };
        var model = new WishlistModel
        {
            Name = "Updated name",
            PublicId = "attacker-public-id",
            OwnerId = "attacker-id",
            Deleted = true
        };

        _mapper.Map(model, entity);

        Assert.Equal("server-public-id", entity.PublicId);
        Assert.Equal("owner-id", entity.OwnerId);
        Assert.False(entity.Deleted);
        Assert.Equal("Updated name", entity.Name);
    }

    [Fact]
    public void MapWishlistItemUpdate_PreservesConnectedAppProvenance()
    {
        var entity = new WishlistItem
        {
            Name = "Wool coat",
            SourceConnectionId = 7,
            SourceAppName = "Studio Notebook",
            ExternalId = "coat-1"
        };
        var model = new WishlistItemModel { Name = "Wool coat, charcoal", SourceAppName = "Spoofed app" };

        _mapper.Map(model, entity);

        Assert.Equal("Wool coat, charcoal", entity.Name);
        Assert.Equal(7, entity.SourceConnectionId);
        Assert.Equal("Studio Notebook", entity.SourceAppName);
        Assert.Equal("coat-1", entity.ExternalId);
        Assert.Equal("Studio Notebook", _mapper.Map<WishlistItemModel>(entity).SourceAppName);
    }

    [Fact]
    public void MapEventUpdate_PreservesServerOwnedFields()
    {
        var entity = new Event
        {
            Name = "Original event",
            CreatedBy = new ApplicationUser(),
            PublicId = "server-public-id",
            Deleted = false
        };
        var model = new EventModel
        {
            Name = "Updated event",
            PublicId = "attacker-public-id",
            Deleted = true
        };

        _mapper.Map(model, entity);

        Assert.Equal("server-public-id", entity.PublicId);
        Assert.False(entity.Deleted);
        Assert.Equal("Updated event", entity.Name);
    }
}