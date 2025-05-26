# Technical Implementation Details: Social Features

This document outlines the technical implementation of social features in OpenWish, including entity relationships, service architecture, and data flow.

## Entity Relationship Diagram

```mermaid
erDiagram
    ApplicationUser {
        string Id PK
        string UserName
        string Email
    }
    
    Friend {
        string Id PK
        string UserId FK
        string FriendUserId FK
        DateTime CreatedAt
    }
    
    FriendRequest {
        string Id PK
        string RequesterId FK
        string RecipientId FK
        DateTime CreatedAt
        int Status
    }
    
    Wishlist {
        string Id PK
        string UserId FK
        string Title
        bool IsPublic
    }
    
    WishlistItem {
        string Id PK
        string WishlistId FK
        string Name
        string Description
        decimal Price
        string Link
        int Priority
    }
    
    WishlistPermission {
        string Id PK
        string WishlistId FK
        string UserId FK
        int PermissionLevel
        DateTime CreatedAt
        DateTime ExpiresAt
        string ShareableLink
    }
    
    ItemComment {
        string Id PK
        string ItemId FK
        string UserId FK
        string Content
        DateTime CreatedAt
    }
    
    ItemReservation {
        string Id PK
        string ItemId FK
        string UserId FK
        bool IsAnonymous
        DateTime ReservedAt
    }
    
    ActivityLog {
        string Id PK
        string UserId FK
        int ActivityType
        string EntityId
        string Details
        DateTime Timestamp
    }
    
    ApplicationUser ||--o{ Friend : "has"
    ApplicationUser ||--o{ FriendRequest : "sends/receives"
    ApplicationUser ||--o{ Wishlist : "owns"
    Wishlist ||--o{ WishlistItem : "contains"
    Wishlist ||--o{ WishlistPermission : "has"
    WishlistItem ||--o{ ItemComment : "has"
    WishlistItem ||--o{ ItemReservation : "has"
    ApplicationUser ||--o{ ActivityLog : "generates"
```

## Service Architecture

```mermaid
flowchart TD
    Client[Web UI / Client UI]
    
    API[Controller Layer]
    
    FriendSvc[Friend Service]
    WishlistSvc[Wishlist Service]
    NotificationSvc[Notification Service]
    ActivitySvc[Activity Service]
    
    DB[(Database)]
    
    Client <--> API
    
    API --> FriendSvc
    API --> WishlistSvc
    API --> NotificationSvc
    
    FriendSvc --> DB
    WishlistSvc --> DB
    NotificationSvc --> DB
    ActivitySvc --> DB
    
    FriendSvc --> ActivitySvc
    WishlistSvc --> ActivitySvc
    ActivitySvc --> NotificationSvc
```

## Friend Request State Diagram

```mermaid
stateDiagram-v2
    [*] --> Pending: Create Request
    Pending --> Accepted: Accept
    Pending --> Rejected: Reject
    Accepted --> [*]
    Rejected --> [*]
```

## Wishlist Sharing Sequence Diagram

```mermaid
sequenceDiagram
    actor Owner
    participant UI as User Interface
    participant WS as Wishlist Service
    participant DB as Database
    participant NS as Notification Service
    participant Friend
    
    Owner->>UI: Share Wishlist
    UI->>WS: ShareWishlist(wishlistId, friendId, permission)
    WS->>DB: Create Permission Record
    WS->>NS: Create Notification
    NS->>DB: Store Notification
    NS-->>Friend: Notify (real-time)
    WS-->>UI: Confirmation
    UI-->>Owner: Success Message
```

## Item Reservation Process

```mermaid
flowchart TD
    Start([Start]) --> ViewItem[View Item]
    ViewItem --> CheckStatus{Item Reserved?}
    
    CheckStatus -->|No| Reserve[Reserve Item]
    CheckStatus -->|Yes by me| Cancel[Cancel Reservation]
    CheckStatus -->|Yes by other| End([End])
    
    Reserve --> SelectType{Select Type}
    SelectType -->|Anonymous| AnonReserve[Create Anonymous Reservation]
    SelectType -->|Visible| VisibleReserve[Create Visible Reservation]
    
    AnonReserve --> LogActivity[Log Activity]
    VisibleReserve --> LogActivity
    Cancel --> RemoveRes[Remove Reservation]
    RemoveRes --> LogActivity
    
    LogActivity --> Notify[Generate Notification]
    Notify --> UpdateUI[Update UI]
    UpdateUI --> End
```

## Technical Components

### Friend Management
- `FriendService` implements `IFriendService` with methods for:
  - Sending friend requests
  - Accepting/rejecting requests
  - Retrieving friend lists
  - Searching for potential friends
  - Sending email invitations
  - Creating friendships from email invitations
- Friend relationships are bi-directional with a single record

### Email Invitation Flow

```mermaid
sequenceDiagram
    actor User
    participant UI as User Interface
    participant FS as Friend Service
    participant Email as Email Service
    participant DB as Database
    participant Registration as Registration Page
    
    User->>UI: Enter email to invite
    UI->>FS: SendFriendInviteByEmailAsync(senderId, email)
    
    alt User exists
        FS->>DB: Check if user exists
        DB-->>FS: User found
        FS->>FS: SendFriendRequestAsync(senderId, existingUserId)
    else User does not exist
        FS->>DB: Check if user exists
        DB-->>FS: User not found
        FS->>Email: Send invitation email with link
        Email-->>User: Invitation email with registration link
    end
    
    User->>Registration: Click invitation link
    Registration->>Registration: Parse invite data (email|inviterId)
    Registration->>Registration: Pre-fill email field
    User->>Registration: Complete registration form
    Registration->>DB: Create new user
    Registration->>FS: CreateFriendshipFromInviteAsync(newUserId, inviterId)
    FS->>DB: Create bidirectional friendship records
    FS->>DB: Create notification for inviter
```

### Wishlist Sharing
- `WishlistPermission` entity stores:
  - Direct user permissions
  - Shareable link tokens (when generated)
  - Permission expiration dates
  - Access level (view/edit)
- URL-based sharing uses secure tokens with configurable expiration

### Item Interactions
- Comments implement a simple thread model with creation timestamps
- Reservations track the reserving user and anonymity preferences
- Changes trigger activity logs and notifications

### Activity Logging
- `ActivityService` centrally logs all social interactions
- Entries include:
  - User ID
  - Activity type (enum)
  - Entity reference (item, wishlist, etc.)
  - Timestamp
  - Additional context

### Notification System
- Real-time notifications via SignalR
- Persistent storage for history
- Types include:
  - Friend requests
  - Wishlist shares
  - Item comments
  - Item reservations