using System.Net.Http.Json;
using OpenWish.Shared.Dashboard;
using OpenWish.Shared.Services;

namespace OpenWish.Web.Client.Services;

public class DashboardHttpClientService(HttpClient httpClient) : IDashboardService
{
    private const string BaseUrl = "api/dashboard";

    public async Task<FirstRunChecklist> GetFirstRunChecklistAsync(string userId) =>
        await httpClient.GetFromJsonAsync<FirstRunChecklist>($"{BaseUrl}/checklist") ?? FirstRunChecklist.Hidden;

    public async Task DismissFirstRunChecklistAsync(string userId)
    {
        var response = await httpClient.PostAsync($"{BaseUrl}/checklist/dismiss", null);
        response.EnsureSuccessStatusCode();
    }
}