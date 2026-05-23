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
    private readonly ToastService _toast;
    private readonly string _baseUrl;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiService(HttpClient http, AuthStateService auth, ToastService toast, IConfiguration config)
    {
        _http = http;
        _auth = auth;
        _toast = toast;
        var apiRoot = (config["ApiBaseUrl"] ?? "https://destinoperu-production.up.railway.app/").TrimEnd('/');
        _baseUrl = $"{apiRoot}/api";
    }

    private void PrepareRequest() => _auth.ApplyAuth(_http);

    public async Task<ApiResult<PagedResult<TourDto>>> SearchToursAsync(
        string? department = null, string? location = null, string? adventureType = null,
        DateTime? fromDate = null, decimal? maxPrice = null, int page = 1)
    {
        try
        {
            PrepareRequest();
            var q = new List<string> { $"page={page}", "pageSize=12" };
            if (!string.IsNullOrWhiteSpace(department)) q.Add($"department={Uri.EscapeDataString(department)}");
            if (!string.IsNullOrWhiteSpace(location)) q.Add($"location={Uri.EscapeDataString(location)}");
            if (!string.IsNullOrWhiteSpace(adventureType)) q.Add($"adventureType={Uri.EscapeDataString(adventureType)}");
            if (fromDate.HasValue) q.Add($"fromDate={fromDate.Value:O}");
            if (maxPrice.HasValue) q.Add($"maxPrice={maxPrice.Value}");
            var response = await _http.GetAsync($"{_baseUrl}/tours?{string.Join("&", q)}");
            if (!response.IsSuccessStatusCode)
                return Fail<PagedResult<TourDto>>($"Error al cargar tours ({(int)response.StatusCode}).");
            var result = await response.Content.ReadFromJsonAsync<PagedResult<TourDto>>(JsonOptions);
            return ApiResult<PagedResult<TourDto>>.Ok(result ?? new PagedResult<TourDto>());
        }
        catch (Exception ex) { return Fail<PagedResult<TourDto>>("No se pudo conectar con el servidor.", ex); }
    }

    public async Task<ApiResult<TourDto>> GetTourBySlugAsync(string slug)
    {
        try
        {
            PrepareRequest();
            var response = await _http.GetAsync($"{_baseUrl}/tours/slug/{slug}");
            if (!response.IsSuccessStatusCode) return Fail<TourDto>("Tour no encontrado.");
            var wrapper = await response.Content.ReadFromJsonAsync<ApiResponse<TourDto>>(JsonOptions);
            return wrapper?.Data is not null ? ApiResult<TourDto>.Ok(wrapper.Data) : Fail<TourDto>(wrapper?.Message ?? "No encontrado");
        }
        catch (Exception ex) { return Fail<TourDto>("Error al cargar tour.", ex); }
    }

    public async Task<ApiResult<string>> UploadImageAsync(Stream stream, string fileName, string folder)
    {
        try
        {
            PrepareRequest();
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(stream), "file", fileName);
            var response = await _http.PostAsync($"{_baseUrl}/media/upload?folder={folder}", content);
            if (!response.IsSuccessStatusCode) return Fail<string>("Error al subir imagen.");
            var json = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>(JsonOptions);
            return json?.TryGetValue("url", out var url) == true ? ApiResult<string>.Ok(url!) : Fail<string>("Respuesta invalida.");
        }
        catch (Exception ex) { return Fail<string>("Error de subida.", ex); }
    }

    public async Task<ApiResult<AdminMetricsDto>> GetAdminMetricsAsync()
    {
        try
        {
            PrepareRequest();
            var response = await _http.GetAsync($"{_baseUrl}/admin/metrics");
            if (!response.IsSuccessStatusCode) return Fail<AdminMetricsDto>("Sin acceso admin.");
            var m = await response.Content.ReadFromJsonAsync<AdminMetricsDto>(JsonOptions);
            return m is not null ? ApiResult<AdminMetricsDto>.Ok(m) : Fail<AdminMetricsDto>("Sin datos");
        }
        catch (Exception ex) { return Fail<AdminMetricsDto>("Error admin.", ex); }
    }

    public async Task<ApiResult<PaymentDto>> SubmitVoucherAsync(SubmitVoucherRequest request)
    {
        try
        {
            PrepareRequest();
            var response = await _http.PostAsJsonAsync($"{_baseUrl}/payments/voucher", request);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaymentDto>>(JsonOptions);
            if (result?.Success == true && result.Data is not null) return ApiResult<PaymentDto>.Ok(result.Data);
            return Fail<PaymentDto>(result?.Message ?? "Error al enviar voucher.");
        }
        catch (Exception ex) { return Fail<PaymentDto>("Error de pago.", ex); }
    }

    public async Task<ApiResult<TourDto>> GetTourAsync(int id)
    {
        try
        {
            PrepareRequest();
            var response = await _http.GetAsync($"{_baseUrl}/tours/{id}");
            if (!response.IsSuccessStatusCode) return Fail<TourDto>($"Tour no encontrado ({(int)response.StatusCode}).");
            var wrapper = await response.Content.ReadFromJsonAsync<ApiResponse<TourDto>>(JsonOptions);
            return wrapper?.Data is not null ? ApiResult<TourDto>.Ok(wrapper.Data) : Fail<TourDto>(wrapper?.Message ?? "Tour no disponible.");
        }
        catch (Exception ex) { return Fail<TourDto>("Error al cargar el tour.", ex); }
    }

    public async Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"{_baseUrl}/auth/login", request);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions);
            if (result?.Success == true && result.Data is not null)
                return ApiResult<AuthResponse>.Ok(result.Data);
            return Fail<AuthResponse>(result?.Message ?? "Credenciales incorrectas.");
        }
        catch (Exception ex)
        {
            return Fail<AuthResponse>("Error de conexion al iniciar sesion.", ex);
        }
    }

    public async Task<ApiResult<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync($"{_baseUrl}/auth/register", request);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(JsonOptions);
            if (result?.Success == true && result.Data is not null)
                return ApiResult<AuthResponse>.Ok(result.Data);
            return Fail<AuthResponse>(result?.Message ?? "No se pudo registrar.");
        }
        catch (Exception ex)
        {
            return Fail<AuthResponse>("Error de conexion al registrarse.", ex);
        }
    }

    public async Task<ApiResult<ReservationDto>> CreateReservationAsync(CreateReservationRequest request)
    {
        try
        {
            PrepareRequest();
            var response = await _http.PostAsJsonAsync($"{_baseUrl}/reservations", request);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<ReservationDto>>(JsonOptions);
            if (result?.Success == true && result.Data is not null)
                return ApiResult<ReservationDto>.Ok(result.Data);
            return Fail<ReservationDto>(result?.Message ?? "No se pudo crear la reserva.");
        }
        catch (Exception ex)
        {
            return Fail<ReservationDto>("Error al procesar la reserva.", ex);
        }
    }

    public async Task<ApiResult<List<ReservationDto>>> GetUserReservationsAsync()
    {
        try
        {
            PrepareRequest();
            var response = await _http.GetAsync($"{_baseUrl}/reservations/user");
            if (!response.IsSuccessStatusCode)
                return Fail<List<ReservationDto>>($"Error al cargar reservas ({(int)response.StatusCode}).");
            var result = await response.Content.ReadFromJsonAsync<List<ReservationDto>>(JsonOptions);
            return ApiResult<List<ReservationDto>>.Ok(result ?? []);
        }
        catch (Exception ex)
        {
            return Fail<List<ReservationDto>>("Error al cargar tus reservas.", ex);
        }
    }

    public async Task<ApiResult<PaymentDto>> ProcessPaymentAsync(CreatePaymentRequest request)
    {
        try
        {
            PrepareRequest();
            var response = await _http.PostAsJsonAsync($"{_baseUrl}/payments", request);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaymentDto>>(JsonOptions);
            if (result?.Success == true && result.Data is not null)
                return ApiResult<PaymentDto>.Ok(result.Data);
            return Fail<PaymentDto>(result?.Message ?? "No se pudo procesar el pago.");
        }
        catch (Exception ex)
        {
            return Fail<PaymentDto>("Error al procesar el pago.", ex);
        }
    }

    private ApiResult<T> Fail<T>(string message, Exception? ex = null)
    {
        var detail = ex is HttpRequestException ? " Verifica tu conexion o la API en Railway." : "";
        var full = message + detail;
        _toast.ShowError(full);
        return ApiResult<T>.Fail(full);
    }
}
