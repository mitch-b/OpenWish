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
            .ForMember(dest => dest.PublicId, opt => opt.Condition(src => !string.IsNullOrEmpty(src.PublicId))) // only map PublicId if not empty
            .ForMember(dest => dest.Items, opt => opt.Ignore()) // do not map from DtoModel to EF entity
            .ForMember(dest => dest.Owner, opt => opt.Ignore()) // do not map from DtoModel to EF entity
            .ForMember(dest => dest.Event, opt => opt.Ignore())
            .ForMember(dest => dest.EventId, opt => opt.Ignore())
            .ReverseMap()
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))  // map from EF entity to DtoModel
            .ForMember(dest => dest.Owner, opt => opt.MapFrom(src => src.Owner)); // map from EF entity to DtoModel
        CreateMap<WishlistItemModel, WishlistItem>()
            .ForMember(dest => dest.PublicId, opt => opt.Condition(src => !string.IsNullOrEmpty(src.PublicId))) // only map PublicId if not empty
                                                                                                                // Prevent client-supplied WishlistId from overwriting the existing FK on update
            .ForMember(dest => dest.WishlistId, opt => opt.Ignore())
            .ReverseMap();
        CreateMap<EventModel, Event>()
            .ForMember(dest => dest.PublicId, opt => opt.Condition(src => !string.IsNullOrEmpty(src.PublicId))) // only map PublicId if not empty
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => string.Join(',', src.Tags)))
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.EventUsers, opt => opt.Ignore()) // do not map from DtoModel to EF entity
            .ForMember(dest => dest.EventWishlists, opt => opt.Ignore())
            .ForMember(dest => dest.GiftExchanges, opt => opt.Ignore())
            .ForMember(dest => dest.PairingRules, opt => opt.Ignore())
            .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Date.ToUniversalTime()))
            .ReverseMap()
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => (src.Tags ?? string.Empty).Split(",", StringSplitOptions.RemoveEmptyEntries)))
            .ForMember(dest => dest.EventUsers, opt => opt.MapFrom(src => src.EventUsers)) // map from EF entity to DtoModel
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.EventWishlists, opt => opt.MapFrom(src => src.EventWishlists))  // map from EF entity to DtoModel
            .ForMember(dest => dest.GiftExchanges, opt => opt.MapFrom(src => src.GiftExchanges))
            .ForMember(dest => dest.PairingRules, opt => opt.MapFrom(src => src.PairingRules));
        CreateMap<EventUserModel, EventUser>()
            .ForMember(dest => dest.PublicId, opt => opt.Condition(src => !string.IsNullOrEmpty(src.PublicId))) // only map PublicId if not empty
            .ReverseMap();

        // Social feature mappings
        CreateMap<FriendModel, Friend>()
            .ForMember(dest => dest.PublicId, opt => opt.Condition(src => !string.IsNullOrEmpty(src.PublicId))) // only map PublicId if not empty
            .ReverseMap();
        CreateMap<FriendRequestModel, FriendRequest>()
            .ForMember(dest => dest.PublicId, opt => opt.Condition(src => !string.IsNullOrEmpty(src.PublicId))) // only map PublicId if not empty
            .ReverseMap();
        CreateMap<WishlistPermissionModel, WishlistPermission>()
            .ForMember(dest => dest.PublicId, opt => opt.Condition(src => !string.IsNullOrEmpty(src.PublicId))) // only map PublicId if not empty
            .ReverseMap();
        CreateMap<ItemCommentModel, ItemComment>()
            .ForMember(dest => dest.PublicId, opt => opt.Condition(src => !string.IsNullOrEmpty(src.PublicId))) // only map PublicId if not empty
            .ReverseMap();
        CreateMap<ItemReservationModel, ItemReservation>()
            .ForMember(dest => dest.PublicId, opt => opt.Condition(src => !string.IsNullOrEmpty(src.PublicId))) // only map PublicId if not empty
            .ReverseMap();
        CreateMap<ActivityLogModel, ActivityLog>()
            .ForMember(dest => dest.PublicId, opt => opt.Condition(src => !string.IsNullOrEmpty(src.PublicId))) // only map PublicId if not empty
            .ReverseMap();
        CreateMap<NotificationModel, Notification>()
            .ForMember(dest => dest.PublicId, opt => opt.Condition(src => !string.IsNullOrEmpty(src.PublicId))) // only map PublicId if not empty
            .ForMember(dest => dest.ActionData, opt => opt.MapFrom(src => NotificationActionMapper.Serialize(src.Action)))
            .ReverseMap()
            .ForMember(dest => dest.Action, opt => opt.MapFrom(src => NotificationActionMapper.Deserialize(src.ActionData)));
        CreateMap<GiftExchangeModel, GiftExchange>()
            .ForMember(dest => dest.PublicId, opt => opt.Condition(src => !string.IsNullOrEmpty(src.PublicId))) // only map PublicId if not empty
            .ReverseMap();
        CreateMap<CustomPairingRuleModel, CustomPairingRule>()
            .ForMember(dest => dest.PublicId, opt => opt.Condition(src => !string.IsNullOrEmpty(src.PublicId))) // only map PublicId if not empty
            .ReverseMap();
    }
}