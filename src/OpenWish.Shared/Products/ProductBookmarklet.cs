namespace OpenWish.Shared.Products;

/// <summary>
/// Builds the "Add to OpenWish" bookmarklet. It runs on the store page in the person's own browser,
/// so it can read product details from stores that block OpenWish's server.
/// </summary>
public static class ProductBookmarklet
{
    public const string CapturePath = "add";

    // A javascript: URL is one line, and browsers percent-decode it before running it,
    // so this script avoids line breaks, comments, '%', and '#'.
    private const string Script =
        "(()=>{" +
        "const d=document,l=location;" +
        "if(!/^https?:$/.test(l.protocol)){alert('Open a product page, then use Add to OpenWish.');return}" +
        "const m=n=>{const e=d.querySelector('meta[property=\"'+n+'\"],meta[name=\"'+n+'\"],meta[itemprop=\"'+n+'\"]');return e&&e.content||''};" +
        "const t=i=>{const e=d.getElementById(i);return e?(e.textContent||'').trim():''};" +
        "let j=null;" +
        "const f=x=>{if(j||!x||typeof x!=='object')return;if(Array.isArray(x)){x.forEach(f);return}" +
        "const y=[].concat(x['@type']);if(y.includes('Product')||y.includes('ProductGroup')){j=x;return}f(x['@graph'])};" +
        "d.querySelectorAll('script[type=\"application/ld+json\"]').forEach(s=>{try{f(JSON.parse(s.textContent))}catch(e){}});" +
        "j=j||{};" +
        "const o=[].concat(j.offers||[])[0]||{},g=[].concat(j.image||[])[0],a=d.getElementById('landingImage'),c=d.querySelector('.a-price .a-offscreen');" +
        "const p={url:l.href," +
        "title:j.name||m('og:title')||t('productTitle')||d.title," +
        "description:j.description||m('og:description')||m('description')," +
        "price:o.price||o.lowPrice||m('product:price:amount')||m('og:price:amount')||(c?c.textContent:'')," +
        "image:(g&&(g.url||g))||m('og:image')||m('twitter:image')||(a?a.getAttribute('data-old-hires')||a.src:'')};" +
        "const q=new URLSearchParams();" +
        "for(const k in p){const v=String(p[k]||'').trim();if(v)q.set(k,v.slice(0,k==='description'?600:2000))}" +
        "const u=__OPENWISH_APP__+'" + CapturePath + "?'+q;" +
        "if(!window.open(u,'_blank'))l.href=u" +
        "})()";

    /// <summary>Returns the bookmarklet address for an OpenWish site, such as https://wishes.example/.</summary>
    public static string Create(Uri appBaseUri)
    {
        ArgumentNullException.ThrowIfNull(appBaseUri);
        if (!appBaseUri.IsAbsoluteUri || (appBaseUri.Scheme != Uri.UriSchemeHttp && appBaseUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("The OpenWish address must be an absolute http or https URL.", nameof(appBaseUri));
        }

        var origin = appBaseUri.GetLeftPart(UriPartial.Path);
        if (!origin.EndsWith('/'))
        {
            origin += "/";
        }

        // Escape characters that would break out of the JavaScript string or be decoded by the browser.
        var appLiteral = "'" + origin
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("'", "\\'", StringComparison.Ordinal)
            .Replace("%", "%25", StringComparison.Ordinal)
            .Replace("#", "%23", StringComparison.Ordinal) + "'";
        return "javascript:" + Script.Replace("__OPENWISH_APP__", appLiteral, StringComparison.Ordinal);
    }
}