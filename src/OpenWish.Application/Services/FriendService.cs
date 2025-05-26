using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWish.Application.Models.Configuration;
using OpenWish.Data;
using OpenWish.Data.Entities;
using OpenWish.Shared.Models;
using OpenWish.Shared.Services;

namespace OpenWish.Application.Services;

public class FriendService(ApplicationDbContext context,
    IMapper mapper,
    INotificationService notificationService,
    IAppEmailSender emailSender,
    IOptions<OpenWishSettings> openWishSettings,
    ILogger<FriendService> logger) : IFriendService
{
    private readonly ApplicationDbContext _context = context;
    private readonly IMapper _mapper = mapper;
    private readonly INotificationService _notificationService = notificationService;
    private readonly IAppEmailSender _emailSender = emailSender;
    private readonly string? _baseUri = openWishSettings.Value.BaseUri;
    private readonly ILogger<FriendService> _logger = logger;

    public async Task<IEnumerable<ApplicationUserModel>> GetFriendsAsync(string userId)
    {
        var friends = await _context.Friends
            .Include(f => f.FriendUser)
            .Where(f => f.UserId == userId && !f.Deleted)
            .Select(f => f.FriendUser)
            .ToListAsync();

        return _mapper.Map<IEnumerable<ApplicationUserModel>>(friends);
    }

    public async Task<bool> AreFriendsAsync(string userId, string otherUserId)
    {
        return await _context.Friends
            .AnyAsync(f =>
                ((f.UserId == userId && f.FriendUserId == otherUserId) ||
                (f.UserId == otherUserId && f.FriendUserId == userId)) &&
                !f.Deleted);
    }

    public async Task<bool> RemoveFriendAsync(string userId, string friendId)
    {
        // Find the friendship records in both directions (as friendship is reciprocal)
        var friendship1 = await _context.Friends
            .FirstOrDefaultAsync(f => f.UserId == userId && f.FriendUserId == friendId && !f.Deleted);

        var friendship2 = await _context.Friends
            .FirstOrDefaultAsync(f => f.UserId == friendId && f.FriendUserId == userId && !f.Deleted);

        if (friendship1 == null && friendship2 == null)
        {
            return false;
        }

        // Soft delete both friendship records
        if (friendship1 != null)
        {
            friendship1.Deleted = true;
            friendship1.UpdatedOn = DateTimeOffset.UtcNow;
        }

        if (friendship2 != null)
        {
            friendship2.Deleted = true;
            friendship2.UpdatedOn = DateTimeOffset.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<FriendRequestModel> SendFriendRequestAsync(string requesterId, string receiverId)
    {
        // Check if they are already friends
        if (await AreFriendsAsync(requesterId, receiverId))
        {
            throw new InvalidOperationException("Users are already friends");
        }

        // Check for existing pending requests
        var existingRequest = await _context.FriendRequests
            .FirstOrDefaultAsync(fr =>
                (fr.RequesterId == requesterId && fr.ReceiverId == receiverId ||
                fr.RequesterId == receiverId && fr.ReceiverId == requesterId) &&
                fr.Status == "Pending" && !fr.Deleted);

        if (existingRequest != null)
        {
            throw new InvalidOperationException("A friend request already exists between these users");
        }

        // Create new request
        var newRequest = new FriendRequest
        {
            RequesterId = requesterId,
            ReceiverId = receiverId,
            RequestDate = DateTimeOffset.UtcNow,
            Status = "Pending",
            CreatedOn = DateTimeOffset.UtcNow,
            UpdatedOn = DateTimeOffset.UtcNow
        };

        _context.FriendRequests.Add(newRequest);
        await _context.SaveChangesAsync();

        return _mapper.Map<FriendRequestModel>(newRequest);
    }

    public async Task<IEnumerable<FriendRequestModel>> GetReceivedFriendRequestsAsync(string userId)
    {
        var requests = await _context.FriendRequests
            .Include(fr => fr.Requester)
            .Where(fr => fr.ReceiverId == userId && fr.Status == "Pending" && !fr.Deleted)
            .ToListAsync();

        return _mapper.Map<IEnumerable<FriendRequestModel>>(requests);
    }

    public async Task<IEnumerable<FriendRequestModel>> GetSentFriendRequestsAsync(string userId)
    {
        var requests = await _context.FriendRequests
            .Include(fr => fr.Receiver)
            .Where(fr => fr.RequesterId == userId && fr.Status == "Pending" && !fr.Deleted)
            .ToListAsync();

        return _mapper.Map<IEnumerable<FriendRequestModel>>(requests);
    }

    public async Task<bool> AcceptFriendRequestAsync(int requestId, string userId)
    {
        var request = await _context.FriendRequests
            .FirstOrDefaultAsync(fr => fr.Id == requestId && fr.ReceiverId == userId && fr.Status == "Pending" && !fr.Deleted);

        if (request == null)
        {
            return false;
        }

        // Update request status
        request.Status = "Accepted";
        request.UpdatedOn = DateTimeOffset.UtcNow;

        // Create friendship records in both directions (bidirectional friendship)
        var friendship1 = new Friend
        {
            UserId = request.RequesterId,
            FriendUserId = request.ReceiverId,
            FriendshipDate = DateTimeOffset.UtcNow,
            CreatedOn = DateTimeOffset.UtcNow,
            UpdatedOn = DateTimeOffset.UtcNow
        };

        var friendship2 = new Friend
        {
            UserId = request.ReceiverId,
            FriendUserId = request.RequesterId,
            FriendshipDate = DateTimeOffset.UtcNow,
            CreatedOn = DateTimeOffset.UtcNow,
            UpdatedOn = DateTimeOffset.UtcNow
        };

        _context.Friends.Add(friendship1);
        _context.Friends.Add(friendship2);

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectFriendRequestAsync(int requestId, string userId)
    {
        var request = await _context.FriendRequests
            .FirstOrDefaultAsync(fr => fr.Id == requestId && fr.ReceiverId == userId && fr.Status == "Pending" && !fr.Deleted);

        if (request == null)
        {
            return false;
        }

        // Update request status
        request.Status = "Rejected";
        request.UpdatedOn = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SendFriendInviteByEmailAsync(string senderUserId, string emailAddress)
    {
        // Validate email format
        if (string.IsNullOrWhiteSpace(emailAddress) || !IsValidEmail(emailAddress))
        {
            throw new ArgumentException("Please provide a valid email address.");
        }

        // Check if the user already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == emailAddress);

        if (existingUser != null)
        {
            // If user exists, send a friend request instead
            await SendFriendRequestAsync(senderUserId, existingUser.Id);
            return true;
        }

        // Send invitation email using the application email sender
        var sender = await _context.Users.FirstOrDefaultAsync(u => u.Id == senderUserId);
        if (sender == null)
        {
            return false;
        }

        // Generate an invite link using the configured BaseUri that includes both email and sender ID
        var baseUri = _baseUri?.TrimEnd('/') ?? "";
        var inviteData = $"{emailAddress}|{senderUserId}";
        
        // Ensure proper path format with leading slash but avoiding double slashes
        var registerPath = baseUri.EndsWith("/") ? "Account/Register" : "/Account/Register";
        var inviteLink = $"{baseUri}{registerPath}?invite={Uri.EscapeDataString(inviteData)}";
        
        // Log the generated link for debugging
        _logger.LogInformation("Generated friend invite link: {Link}", inviteLink);
        
        await _emailSender.SendFriendInviteEmailAsync(emailAddress, sender.UserName ?? sender.Email ?? "A friend", inviteLink);

        await _notificationService.CreateNotificationAsync(
            senderUserId,
            senderUserId,  // Target user is the sender (to track sent invites)
            "Friend Invitation Sent",
            $"You've sent a friend invitation to {emailAddress}. They'll receive an email with instructions to join OpenWish.",
            "FriendInvite");

        return true;
    }

    public async Task<bool> SendFriendInvitesByEmailAsync(string senderUserId, IEnumerable<string> emailAddresses)
    {
        bool allSucceeded = true;
        foreach (var email in emailAddresses)
        {
            try
            {
                var success = await SendFriendInviteByEmailAsync(senderUserId, email.Trim());
                if (!success)
                {
                    allSucceeded = false;
                }
            }
            catch
            {
                allSucceeded = false;
            }
        }
        return allSucceeded;
    }

    public async Task<IEnumerable<ApplicationUserModel>> SearchUsersAsync(string searchTerm, string currentUserId, int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(searchTerm) || searchTerm.Length < 2)
        {
            return [];
        }

        // Only search by username for privacy/security reasons
        var users = await _context.Users
            .Where(u => u.Id != currentUserId && u.UserName.Contains(searchTerm))
            .Take(maxResults)
            .ToListAsync();

        return _mapper.Map<IEnumerable<ApplicationUserModel>>(users);
    }

    public async Task<bool> CreateFriendshipFromInviteAsync(string newUserId, string inviterUserId)
    {
        // Check if both users exist
        var newUser = await _context.Users.FindAsync(newUserId);
        var inviter = await _context.Users.FindAsync(inviterUserId);

        if (newUser == null || inviter == null)
        {
            return false;
        }

        // Check if they are already friends
        var existingFriendship = await _context.Friends
            .AnyAsync(f =>
                ((f.UserId == newUserId && f.FriendUserId == inviterUserId) ||
                 (f.UserId == inviterUserId && f.FriendUserId == newUserId)) &&
                !f.Deleted);

        if (existingFriendship)
        {
            return true; // They are already friends
        }

        // Create friendship records in both directions
        var friendship1 = new Friend
        {
            UserId = newUserId,
            FriendUserId = inviterUserId,
            FriendshipDate = DateTimeOffset.UtcNow,
            CreatedOn = DateTimeOffset.UtcNow,
            UpdatedOn = DateTimeOffset.UtcNow
        };

        var friendship2 = new Friend
        {
            UserId = inviterUserId,
            FriendUserId = newUserId,
            FriendshipDate = DateTimeOffset.UtcNow,
            CreatedOn = DateTimeOffset.UtcNow,
            UpdatedOn = DateTimeOffset.UtcNow
        };

        _context.Friends.Add(friendship1);
        _context.Friends.Add(friendship2);

        // Create notification for the inviter
        await _notificationService.CreateNotificationAsync(
            newUserId,
            inviterUserId,
            "Friend Invitation Accepted",
            $"{newUser.UserName ?? newUser.Email} has joined OpenWish and is now your friend.",
            "FriendAccept");

        await _context.SaveChangesAsync();
        return true;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}