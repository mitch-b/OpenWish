using AutoMapper;
using OpenWish.Data.Entities;
using OpenWish.Shared.Models;

namespace OpenWish.Application.Models;

public class OpenWishProfile : Profile
{
    public OpenWishProfile()
    {
        CreateMap<ApplicationUserModel, ApplicationUser>().ReverseMap();
        CreateMap<WishlistModel, Wishlist>()
            .ReverseMap()
            .ForMember(dest => dest.Items, opt => opt.Ignore()) // do not map from DtoModel to EF entity (but will map from EF entity to DtoModel)
            .ForMember(dest => dest.Owner, opt => opt.Ignore()); // do not map from DtoModel to EF entity (but will map from EF entity to DtoModel)
        CreateMap<WishlistItemModel, WishlistItem>().ReverseMap();
        CreateMap<EventModel, Event>()
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => string.Join(',', src.Tags)))
            .ReverseMap()
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => (src.Tags ?? string.Empty).Split(",", StringSplitOptions.RemoveEmptyEntries)))
            .ForMember(dest => dest.EventUsers, opt => opt.Ignore()); // do not map from DtoModel to EF entity (but will map from EF entity to DtoModel)
        CreateMap<EventUserModel, EventUser>().ReverseMap();
    }
}
