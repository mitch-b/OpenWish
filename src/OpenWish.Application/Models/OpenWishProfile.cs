using AutoMapper;
using OpenWish.Data.Entities;
using OpenWish.Shared.Models;

namespace OpenWish.Application.Models;

public class OpenWishProfile : Profile
{
    public OpenWishProfile()
    {
        CreateMap<ApplicationUserModel, ApplicationUser>()
            .ReverseMap();
        CreateMap<WishlistModel, Wishlist>()
            .ForMember(dest => dest.Items, opt => opt.Ignore()) // do not map from DtoModel to EF entity
            .ForMember(dest => dest.Owner, opt => opt.Ignore()) // do not map from DtoModel to EF entity
            .ForMember(dest => dest.Event, opt => opt.Ignore())
            .ForMember(dest => dest.EventId, opt => opt.Ignore())
            .ReverseMap()
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))  // map from EF entity to DtoModel
            .ForMember(dest => dest.Owner, opt => opt.MapFrom(src => src.Owner)); // map from EF entity to DtoModel
        CreateMap<WishlistItemModel, WishlistItem>()
            // Prevent client-supplied WishlistId from overwriting the existing FK on update
            .ForMember(dest => dest.WishlistId, opt => opt.Ignore())
            .ReverseMap();
        CreateMap<EventModel, Event>()
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => string.Join(',', src.Tags)))
            .ForMember(dest => dest.EventUsers, opt => opt.Ignore()) // do not map from DtoModel to EF entity
            .ForMember(dest => dest.EventWishlists, opt => opt.Ignore())
            .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Date.ToUniversalTime()))
            .ReverseMap()
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => (src.Tags ?? string.Empty).Split(",", StringSplitOptions.RemoveEmptyEntries)))
            .ForMember(dest => dest.EventUsers, opt => opt.MapFrom(src => src.EventUsers)) // map from EF entity to DtoModel
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.EventWishlists, opt => opt.MapFrom(src => src.EventWishlists));  // map from EF entity to DtoModel
        CreateMap<EventUserModel, EventUser>()
            .ReverseMap();

        // Social feature mappings
        CreateMap<FriendModel, Friend>().ReverseMap();
        CreateMap<FriendRequestModel, FriendRequest>().ReverseMap();
        CreateMap<WishlistPermissionModel, WishlistPermission>().ReverseMap();
        CreateMap<ItemCommentModel, ItemComment>().ReverseMap();
        CreateMap<ItemReservationModel, ItemReservation>().ReverseMap();
        CreateMap<ActivityLogModel, ActivityLog>().ReverseMap();
        CreateMap<NotificationModel, Notification>().ReverseMap();
    }
}