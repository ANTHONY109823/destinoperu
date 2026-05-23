using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DestinoPeruBlazor.Models;
using Microsoft.JSInterop;

namespace DestinoPeruBlazor.Services;

public class AuthStateService
{
    private const string StorageKey = "destinoperu_auth";
    private readonly IJSRuntime _js;

    public string? Token { get; private set; }
    public string? Name { get; private set; }
    public string? Email { get; private set; }
    public string? Role { get; private set; }
    public int UserId { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrEmpty(Token);
    public event Action? OnChange;

    public AuthStateService(IJSRuntime js) => _js = js;

    public async Task InitializeAsync()
    {
        try
        {
            var json = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            if (string.IsNullOrWhiteSpace(json)) return;
            var auth = JsonSerializer.Deserialize<AuthResponse>(json);
            if (auth is not null && !string.IsNullOrEmpty(auth.Token))
                RestoreAuth(auth);
        }
        catch { /* sin localStorage */ }
    }

    public async Task SetAuthAsync(AuthResponse auth)
    {
        Token = auth.Token;
        Name = auth.Name;
        Email = auth.Email;
        Role = auth.Role;
        UserId = auth.UserId;
        var json = JsonSerializer.Serialize(auth);
        await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        Notify();
    }

    private void RestoreAuth(AuthResponse auth)
    {
        Token = auth.Token;
        Name = auth.Name;
        Email = auth.Email;
        Role = auth.Role;
        UserId = auth.UserId;
        Notify();
    }

    public async Task LogoutAsync()
    {
        Token = Name = Email = Role = null;
        UserId = 0;
        try { await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey); } catch { }
        Notify();
    }

    public void ApplyAuth(HttpClient client)
    {
        client.DefaultRequestHeaders.Remove("Authorization");
        if (IsAuthenticated)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
    }

    private void Notify() => OnChange?.Invoke();
}

public class ApiService
{
    private readonly HttpClient _http;
    private readonly AuthStateService _auth;
    private readonly string _baseUrl;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiService(HttpClient http, AuthStateService auth, IConfiguration config)
    {
        _http = http;
        _auth = auth;
        // Produccion: Railway | Desarrollo: appsettings.json
        _baseUrl = config["ApiBaseUrl"]?.TrimEnd('/') + "/api"
            ?? "https://destinoperu-production.up.railway.app/api";
    }

    private void PrepareRequest()
    {
        _auth.ApplyAuth(_http);
    }

    public async Task<List<TourDto>> GetToursAsync(string? location = null, DateTime? fromDate = null, decimal? maxPrice = null)
    {
        PrepareRequest();
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(location)) query.Add($"location={Uri.EscapeDataString(location)}");
        if (fromDate.HasValue) query.Add($"fromDate={fromDate.Value:O}");
        if (maxPrice.HasValue) query.Add($"maxPrice={maxPrice.Value}");
        var qs = query.Count > 0 ? "?" + string.Join("&", query) : "";
        var result = await _http.GetFromJsonAsync<List<TourDto>>($"{_baseUrl}/tours{qs}", JsonOptions);
        return result ?? [];
    }

    public async Task<ApiResponse<TourDto>?> GetTourAsync(int id)
    {
        PrepareRequest();
        return await _http.GetFromJsonAsync<ApiResponse<TourDto>>($"{_baseUrl}/tours/{id}", JsonOptions);
    }

    public async Task<ApiResponse<AuthResponse>?> LoginAsync(LoginRequest request)
    {
        var response = await _http.PostAsJsonAsync($"{_baseUrl}/auth/login", request);
        return await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions);
    }

    public async Task<ApiResponse<AuthResponse>?> RegisterAsync(RegisterRequest request)
    {
        var response = await _http.PostAsJsonAsync($"{_baseUrl}/auth/register", request);
        return await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions);
    }

    public async Task<ApiResponse<ReservationDto>?> CreateReservationAsync(CreateReservationRequest request)
    {
        PrepareRequest();
        var response = await _http.PostAsJsonAsync($"{_baseUrl}/reservations", request);
        return await response.Content.ReadFromJsonAsync<ApiResponse<ReservationDto>>(JsonOptions);
    }

    public async Task<List<ReservationDto>> GetUserReservationsAsync()
    {
        PrepareRequest();
        var result = await _http.GetFromJsonAsync<List<ReservationDto>>($"{_baseUrl}/reservations/user", JsonOptions);
        return result ?? [];
    }

    public async Task<ApiResponse<PaymentDto>?> ProcessPaymentAsync(CreatePaymentRequest request)
    {
        PrepareRequest();
        var response = await _http.PostAsJsonAsync($"{_baseUrl}/payments", request);
        return await response.Content.ReadFromJsonAsync<ApiResponse<PaymentDto>>(JsonOptions);
    }
}
