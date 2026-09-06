using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenWish.Application.Models;
using OpenWish.Application.Models.Configuration;
using OpenWish.Application.Services;
using OpenWish.Shared.Services;

namespace OpenWish.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenWishApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OpenWishSettings>(configuration.GetSection(nameof(OpenWishSettings)));

        services.AddHttpClient("ProductHttpClient", client =>
        {
            client.DefaultRequestVersion = new Version(2, 0);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "OpenWish/1.0");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests,
            EnableMultipleHttp2Connections = true,
            ConnectCallback = async (context, cancellationToken) =>
            {
                var addresses = await Dns.GetHostAddressesAsync(
                    context.DnsEndPoint.Host,
                    cancellationToken);
                var safeAddresses = addresses.Where(ProductService.IsSafeAddress).ToArray();
                if (safeAddresses.Length == 0 || safeAddresses.Length != addresses.Length)
                {
                    throw new HttpRequestException("The requested address is not publicly routable.");
                }

                var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    await socket.ConnectAsync(safeAddresses, context.DnsEndPoint.Port, cancellationToken);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }
        });

        services.AddScoped<IAppEmailSender, OpenWishEmailSender>();

        // Core services
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<IWishlistService, WishlistService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IProductService, ProductService>();

        // Social features
        services.AddScoped<IFriendService, FriendService>();
        services.AddScoped<INotificationService, NotificationService>();

        services.AddAutoMapper(_ => { }, typeof(OpenWishProfile).Assembly);

        return services;
    }
}