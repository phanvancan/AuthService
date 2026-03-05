using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application2.Models;

namespace Application2.Services;

public class AuthApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthApiClient(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<List<UserItemViewModel>> GetUsersAsync()
    {
        AttachAccessToken();
        var users = await _httpClient.GetFromJsonAsync<List<UserItemViewModel>>("api/users");
        return users ?? new List<UserItemViewModel>();
    }

    public async Task CreateUserAsync(CreateUserInputModel model)
    {
        AttachAccessToken();
        var response = await _httpClient.PostAsJsonAsync("api/users", model);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteUserAsync(Guid id)
    {
        AttachAccessToken();
        var response = await _httpClient.DeleteAsync($"api/users/{id}");
        response.EnsureSuccessStatusCode();
    }

    private void AttachAccessToken()
    {
        var cookieName = _httpContextAccessor.HttpContext?.RequestServices.GetRequiredService<IConfiguration>()["AuthService:AccessTokenCookieName"] ?? "access_token";
        var accessToken = _httpContextAccessor.HttpContext?.Request.Cookies[cookieName];
        _httpClient.DefaultRequestHeaders.Authorization =
            string.IsNullOrWhiteSpace(accessToken)
                ? null
                : new AuthenticationHeaderValue("Bearer", accessToken);
    }
}
