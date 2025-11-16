# AutoMapper PublicId Mapping Fix

## Overview

This document explains the fix implemented for the PublicId field mapping issue in AutoMapper configuration.

## Problem

When creating new entities (Event, Wishlist, etc.) via the API, the `PublicId` field was not being properly initialized, causing the entity to have an empty or invalid PublicId after creation.

### Root Cause

1. **BaseEntity has a C# default value:**
   ```csharp
   public string PublicId { get; set; } = Guid.NewGuid().ToString();
   ```

2. **Model classes initialize PublicId to empty string:**
   ```csharp
   public string PublicId { get; set; } = string.Empty;
   ```

3. **AutoMapper was unconditionally mapping PublicId:**
   - When mapping `EventModel` → `Event`, AutoMapper copied the empty string
   - This overwrote the C# default Guid
   - Result: Entity had `PublicId = ""` instead of a valid GUID

4. **EF Core behavior:**
   - EF Core detected PublicId had a value (even if empty)
   - Included it in the INSERT statement
   - Database constraints failed or returned invalid data

## Solution

Updated the AutoMapper configuration in `OpenWishProfile.cs` to conditionally map `PublicId` only when it contains a non-empty value:

```csharp
CreateMap<EventModel, Event>()
    .ForMember(dest => dest.PublicId, opt => opt.Condition(src => !string.IsNullOrEmpty(src.PublicId)))
    // ... other mappings
```

### How It Works

1. **Creating New Entities** (PublicId is empty in the source):
   - AutoMapper condition fails: `!string.IsNullOrEmpty(src.PublicId)` = false
   - AutoMapper skips mapping the PublicId property
   - Entity retains its C# default value: `Guid.NewGuid().ToString()`
   - Result: Valid GUID is generated and saved ✓

2. **Updating Existing Entities** (PublicId has a value in the source):
   - AutoMapper condition passes: `!string.IsNullOrEmpty(src.PublicId)` = true
   - AutoMapper maps the existing PublicId
   - Result: Existing GUID is preserved ✓

## Affected Entities

The fix was applied to all entity mappings that inherit from `BaseEntity`:

- Event
- EventUser
- Wishlist
- WishlistItem
- Friend
- FriendRequest
- WishlistPermission
- ItemComment
- ItemReservation
- ActivityLog
- Notification

## Testing

### Manual Verification Steps

1. **Create a new event:**
   ```bash
   POST /api/events
   {
     "name": "Test Event",
     "date": "2024-12-25T00:00:00Z",
     "publicId": ""  // Empty string
   }
   ```
   - Expected: Response contains a valid GUID in `publicId` field
   - Expected: Event can be retrieved using the returned `publicId`

2. **Update an existing event:**
   ```bash
   PUT /api/events/{publicId}
   {
     "name": "Updated Event",
     "publicId": "existing-guid-value"
   }
   ```
   - Expected: PublicId remains unchanged
   - Expected: Other fields are updated correctly

### Code-Level Verification

The fix ensures that:
1. Direct entity instantiation (e.g., `new EventUser { ... }`) still works correctly
2. Entities created via AutoMapper get valid PublicIds
3. Entity updates preserve existing PublicIds
4. No breaking changes to existing functionality

## Best Practices

### When Adding New Entity Mappings

Always include the conditional PublicId mapping for entities that inherit from `BaseEntity`:

```csharp
CreateMap<YourModel, YourEntity>()
    .ForMember(dest => dest.PublicId, opt => opt.Condition(src => !string.IsNullOrEmpty(src.PublicId)))
    // ... other mappings
    .ReverseMap();
```

### When Creating Entities Directly

You can create entities directly without worrying about PublicId:

```csharp
// This is fine - PublicId will be auto-generated
var entity = new Event
{
    Name = "Test Event",
    Date = DateTimeOffset.UtcNow
};
```

### When Using AutoMapper

The AutoMapper configuration handles PublicId automatically:

```csharp
// Creating new entity - PublicId will be auto-generated
var eventModel = new EventModel { Name = "Test", PublicId = string.Empty };
var eventEntity = _mapper.Map<Event>(eventModel);
// eventEntity.PublicId will have a valid GUID

// Updating existing entity - PublicId will be preserved
var existingModel = new EventModel { Name = "Test", PublicId = "existing-guid" };
var updatedEntity = _mapper.Map<Event>(existingModel);
// updatedEntity.PublicId will be "existing-guid"
```

## Related Code

- Entity base class: `src/OpenWish.Data/Entities/BaseEntity.cs`
- Model base class: `src/OpenWish.Shared/Models/BaseEntityModel.cs`
- AutoMapper profile: `src/OpenWish.Application/Models/OpenWishProfile.cs`
- Event service: `src/OpenWish.Application/Services/EventService.cs`
- Wishlist service: `src/OpenWish.Application/Services/WishlistService.cs`

## Migration Notes

This fix does not require database migration. It only affects the application-level mapping behavior. Existing data in the database is unaffected.
