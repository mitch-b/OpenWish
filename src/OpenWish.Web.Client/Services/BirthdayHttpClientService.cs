using System.Net.Http.Json;
using OpenWish.Shared.Models;
using OpenWish.Shared.RequestModels;
using OpenWish.Shared.Services;

namespace OpenWish.Web.Client.Services;

public class BirthdayHttpClientService(HttpClient httpClient) : IBirthdayService
{
    private const string BaseUrl = "api/birthdays";

    public async Task<BirthdayModel> GetBirthdayAsync(string userId) =>
        await httpClient.GetFromJsonAsync<BirthdayModel>($"{BaseUrl}/me") ?? new BirthdayModel();

    public async Task SetBirthdayAsync(string userId, int? month, int? day)
    {
        var response = await httpClient.PutAsJsonAsync($"{BaseUrl}/me", new UpdateBirthdayRequest { Month = month, Day = day });
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            throw new ArgumentException(await response.Content.ReadAsStringAsync());
        }

        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<UpcomingBirthdayModel>> GetUpcomingBirthdaysAsync(string userId, int withinDays = 30) =>
        await httpClient.GetFromJsonAsync<List<UpcomingBirthdayModel>>($"{BaseUrl}/upcoming?withinDays={withinDays}") ?? [];
}