using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace OpenWish.Web;

public class ServerHostEnvironment(IWebHostEnvironment env, NavigationManager nav) :
    IWebAssemblyHostEnvironment
{
    public string Environment => env.EnvironmentName;
    public string BaseAddress => nav.BaseUri;

    public bool IsDevelopment() => env.IsDevelopment();
}