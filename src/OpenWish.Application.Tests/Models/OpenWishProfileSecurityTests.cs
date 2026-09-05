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