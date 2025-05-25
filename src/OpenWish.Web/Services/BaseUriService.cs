using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using OpenWish.Application.Models.Configuration;
using System.Text.RegularExpressions;

namespace OpenWish.Web.Services;

public interface IBaseUriService
{
    Uri GetBaseUri();
    Uri ToAbsoluteUri(string relativeUri);
    string ToBaseRelativePath(string uri);
}

public class BaseUriService : IBaseUriService
{
    private readonly IConfiguration _configuration;
    private readonly IOptions<OpenWishSettings> _openWishSettings;
    private readonly NavigationManager _navigationManager;

    public BaseUriService(IConfiguration configuration, IOptions<OpenWishSettings> openWishSettings, NavigationManager navigationManager)
    {
        _configuration = configuration;
        _openWishSettings = openWishSettings;
        _navigationManager = navigationManager;
    }

    public Uri GetBaseUri()
    {
        // read from IOptions<OpenWishSettings> and grab BaseUri - overrides all other ways of deriving
        if (!string.IsNullOrWhiteSpace(_openWishSettings.Value.BaseUri))
        {
            return new Uri(_openWishSettings.Value.BaseUri);
        }

        // when running from a GitHub Codespace, can use the port forwarding domain
        if (_configuration.GetValue<bool>("CODESPACES"))
        {
            var portRegex = new Regex(@"http.+?:(\d+)", RegexOptions.Compiled);
            var portMatch = portRegex.Match(_navigationManager.BaseUri);
            if (portMatch.Success && int.TryParse(portMatch.Groups[1].Value, out var port))
            {
                var codespaceName = _configuration.GetValue<string>("CODESPACE_NAME");
                var portForwardingDomain = _configuration.GetValue<string>("GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN");
                var stitchedUri = $"https://{codespaceName}-{port}.{portForwardingDomain}/";
                return new Uri(stitchedUri, UriKind.Absolute);
            }
        }

        return new Uri(_navigationManager.BaseUri);
    }

    public Uri ToAbsoluteUri(string relativeUri) =>
        new(GetBaseUri(), relativeUri);

    public string ToBaseRelativePath(string uri)
    {
        var baseUri = GetBaseUri();
        var newUri = new Uri(baseUri, uri);
        return baseUri.MakeRelativeUri(newUri).ToString();
    }
}