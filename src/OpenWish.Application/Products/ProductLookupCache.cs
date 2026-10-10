using Microsoft.Extensions.Caching.Memory;
using OpenWish.Shared.Models;

namespace OpenWish.Application.Products;

/// <summary>
/// Remembers recent product lookups so pasting the same link again, or a friend adding the
/// same gift, is instant and does not hit the store a second time.
/// </summary>
public sealed class ProductLookupCache : IDisposable
{
    private const int MaxEntries = 500;

    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = MaxEntries });

    public bool TryGet(Uri url, out ProductModel? product)
    {
        if (_cache.TryGetValue(Key(url), out ProductModel? cached) && cached is not null)
        {
            product = cached with { };
            return true;
        }

        product = null;
        return false;
    }

    public void Set(Uri url, ProductModel product, TimeSpan timeToLive)
    {
        _cache.Set(Key(url), product with { }, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = timeToLive,
            Size = 1
        });
    }

    public void Dispose() => _cache.Dispose();

    private static string Key(Uri url) => url.AbsoluteUri;
}