using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DestinoPeruBlazor.Models;
using Microsoft.JSInterop;

namespace DestinoPeruBlazor.Services;

public class AuthStateService
{
    private const string StorageKey = "destinoperu_auth";
    private const string OriginKey = "destinoperu_auth_origin";
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

    public int? PartnerId { get; private set; }
    public bool Impersonating { get; private set; }

    public async Task SetAuthAsync(AuthResponse auth)
    {
        if (auth.Impersonating && Role == "SuperAdmin" && IsAuthenticated)
        {
            var origin = JsonSerializer.Serialize(new AuthResponse
            {
                Token = Token!, Name = Name!, Email = Email!, Role = Role!, UserId = UserId, PartnerId = PartnerId
            });
            try { await _js.InvokeVoidAsync("localStorage.setItem", OriginKey, origin); } catch { }
        }

        Token = auth.Token;
        Name = auth.Name;
        Email = auth.Email;
        Role = auth.Role;
        UserId = auth.UserId;
        PartnerId = auth.PartnerId;
        Impersonating = auth.Impersonating;
        var json = JsonSerializer.Serialize(auth);
        await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
        Notify();
    }

    public async Task<bool> EndImpersonationAsync()
    {
        try
        {
            var originJson = await _js.InvokeAsync<string?>("localStorage.getItem", OriginKey);
            if (string.IsNullOrWhiteSpace(originJson)) return false;
            var origin = JsonSerializer.Deserialize<AuthResponse>(originJson);
            if (origin is null || string.IsNullOrEmpty(origin.Token)) return false;
            await _js.InvokeVoidAsync("localStorage.removeItem", OriginKey);
            RestoreAuth(origin);
            await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, originJson);
            Notify();
            return true;
        }
        catch { return false; }
    }

    private void RestoreAuth(AuthResponse auth)
    {
        Token = auth.Token;
        Name = auth.Name;
        Email = auth.Email;
        Role = auth.Role;
        UserId = auth.UserId;
        PartnerId = auth.PartnerId;
        Impersonating = auth.Impersonating;
        Notify();
    }

    public async Task LogoutAsync()
    {
        Token = Name = Email = Role = null;
        UserId = 0;
        PartnerId = null;
        Impersonating = false;
        try { await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey); } catch { }
        Notify();
    }

    public void ApplyAuth(HttpClient client)
    {
        client.DefaultRequestHeaders.Remove("Authorization");
        client.DefaultRequestHeaders.Remove("X-Partner-Id");
        if (IsAuthenticated)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
            if (PartnerId.HasValue)
                client.DefaultRequestHeaders.Add("X-Partner-Id", PartnerId.Value.ToString());
        }
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
        DateTime? fromDate = null, DateTime? toDate = null, int page = 1, decimal? maxPrice = null)
    {
        try
        {
            PrepareRequest();
            var q = new List<string> { $"page={page}", "pageSize=12" };
            if (!string.IsNullOrWhiteSpace(department)) q.Add($"department={Uri.EscapeDataString(department)}");
            if (!string.IsNullOrWhiteSpace(location)) q.Add($"location={Uri.EscapeDataString(location)}");
            if (!string.IsNullOrWhiteSpace(adventureType)) q.Add($"adventureType={Uri.EscapeDataString(adventureType)}");
            if (fromDate.HasValue) q.Add($"fromDate={fromDate.Value:O}");
            if (toDate.HasValue) q.Add($"toDate={toDate.Value:O}");
            if (maxPrice.HasValue) q.Add($"maxPrice={maxPrice.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
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

    public async Task<ApiResult<AgencyPublicProfileDto>> GetAgencyBySlugAsync(string slug)
    {
        try
        {
            PrepareRequest();
            var response = await _http.GetAsync($"{_baseUrl}/agencies/{Uri.EscapeDataString(slug)}");
            if (!response.IsSuccessStatusCode) return Fail<AgencyPublicProfileDto>("Agencia no encontrada.", showToast: false);
            var wrapper = await response.Content.ReadFromJsonAsync<ApiResponse<AgencyPublicProfileDto>>(JsonOptions);
            return wrapper?.Data is not null
                ? ApiResult<AgencyPublicProfileDto>.Ok(wrapper.Data)
                : Fail<AgencyPublicProfileDto>(wrapper?.Message ?? "Agencia no encontrada.", showToast: false);
        }
        catch (Exception ex) { return Fail<AgencyPublicProfileDto>("No se pudo cargar la agencia.", ex, showToast: false); }
    }

    public async Task<ApiResult<List<PopularDestinationDto>>> GetPublicDestinationsAsync() =>
        await GetJsonAsync<List<PopularDestinationDto>>($"{_baseUrl}/destinations", "Destinos no disponibles.", showToast: false);

    public async Task<ApiResult<List<PopularDestinationDto>>> GetAdminDestinationsAsync() =>
        await GetJsonAsync<List<PopularDestinationDto>>($"{_baseUrl}/superadmin/destinations", "No se pudieron cargar destinos.", showToast: false);

    public async Task<ApiResult<bool>> CreateDestinationAsync(UpsertPopularDestinationRequest request)
    {
        try
        {
            PrepareRequest();
            var response = await _http.PostAsJsonAsync($"{_baseUrl}/superadmin/destinations", request);
            return response.IsSuccessStatusCode ? ApiResult<bool>.Ok(true) : Fail<bool>("No se pudo crear el destino.");
        }
        catch (Exception ex) { return Fail<bool>("Error al crear destino.", ex); }
    }

    public async Task<ApiResult<bool>> UpdateDestinationAsync(int id, UpsertPopularDestinationRequest request)
    {
        try
        {
            PrepareRequest();
            var response = await _http.PutAsJsonAsync($"{_baseUrl}/superadmin/destinations/{id}", request);
            return response.IsSuccessStatusCode ? ApiResult<bool>.Ok(true) : Fail<bool>("No se pudo actualizar el destino.");
        }
        catch (Exception ex) { return Fail<bool>("Error al actualizar destino.", ex); }
    }

    public async Task<ApiResult<bool>> DeleteDestinationAsync(int id)
    {
        try
        {
            PrepareRequest();
            var response = await _http.DeleteAsync($"{_baseUrl}/superadmin/destinations/{id}");
            return response.IsSuccessStatusCode ? ApiResult<bool>.Ok(true) : Fail<bool>("No se pudo eliminar el destino.");
        }
        catch (Exception ex) { return Fail<bool>("Error al eliminar destino.", ex); }
    }

    public async Task<ApiResult<bool>> MoveDestinationAsync(int id, int direction)
    {
        try
        {
            PrepareRequest();
            var response = await _http.PutAsync($"{_baseUrl}/superadmin/destinations/{id}/move?direction={direction}", null);
            return response.IsSuccessStatusCode ? ApiResult<bool>.Ok(true) : Fail<bool>("No se pudo reordenar.");
        }
        catch (Exception ex) { return Fail<bool>("Error al reordenar.", ex); }
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

    public async Task<ApiResult<AdminMetricsDto>> GetAdminMetricsAsync() =>
        await GetJsonAsync<AdminMetricsDto>($"{_baseUrl}/admin/metrics", "Sin acceso admin.");

    public async Task<ApiResult<SuperAdminMetricsDto>> GetSuperAdminMetricsAsync() =>
        await GetJsonAsync<SuperAdminMetricsDto>($"{_baseUrl}/superadmin/metrics", "Sin acceso Super Admin.", showToast: false);

    public async Task<ApiResult<List<PartnerListItemDto>>> GetSuperAdminPartnersAsync() =>
        await GetJsonAsync<List<PartnerListItemDto>>($"{_baseUrl}/superadmin/partners", "No se pudieron cargar agencias.", showToast: false);

    public async Task<ApiResult<AgencyProfileDto>> GetAgencyProfileAsync() =>
        await GetJsonAsync<AgencyProfileDto>($"{_baseUrl}/agency/profile", "Perfil de agencia no disponible.", showToast: false);

    public async Task<ApiResult<ApiResponse<object>>> CreateAgencyAsync(CreateAgencyRequest request)
    {
        try
        {
            PrepareRequest();
            var response = await _http.PostAsJsonAsync($"{_baseUrl}/superadmin/partners", request);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>(JsonOptions);
            if (response.IsSuccessStatusCode && result?.Success == true)
                return ApiResult<ApiResponse<object>>.Ok(result);
            return Fail<ApiResponse<object>>(result?.Message ?? "No se pudo crear la agencia.");
        }
        catch (Exception ex) { return Fail<ApiResponse<object>>("Error al crear agencia.", ex); }
    }

    public async Task<ApiResult<bool>> SuspendPartnerAsync(int partnerId, bool suspend = true)
    {
        try
        {
            PrepareRequest();
            var response = await _http.PutAsync($"{_baseUrl}/superadmin/partners/{partnerId}/suspend?suspend={suspend}", null);
            return response.IsSuccessStatusCode ? ApiResult<bool>.Ok(true) : Fail<bool>("No se pudo actualizar el estado.");
        }
        catch (Exception ex) { return Fail<bool>("Error al suspender agencia.", ex); }
    }

    public async Task<ApiResult<AuthResponse>> ImpersonateAsync(int userId)
    {
        try
        {
            if (userId <= 0) return Fail<AuthResponse>("Usuario admin no válido para esta agencia.");
            PrepareRequest();
            var response = await _http.PostAsync($"{_baseUrl}/superadmin/impersonate/{userId}", null);
            var body = await response.Content.ReadAsStringAsync();
            ApiResponse<AuthResponse>? result = null;
            try { result = System.Text.Json.JsonSerializer.Deserialize<ApiResponse<AuthResponse>>(body, JsonOptions); } catch { /* ignore */ }
            if (response.IsSuccessStatusCode && result?.Success == true && result.Data is not null)
                return ApiResult<AuthResponse>.Ok(result.Data);
            var msg = result?.Message ?? (response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? "Sesión expirada. Vuelve a ingresar como Super Admin."
                : $"No se pudo suplantar ({(int)response.StatusCode}).");
            return Fail<AuthResponse>(msg);
        }
        catch (Exception ex) { return Fail<AuthResponse>($"Error de suplantación: {ex.Message}", ex); }
    }

    public async Task<ApiResult<SuperAdminDashboardDto>> GetSuperAdminDashboardAsync() =>
        await GetJsonAsync<SuperAdminDashboardDto>($"{_baseUrl}/superadmin/dashboard", "Dashboard no disponible.", showToast: false);

    public async Task<ApiResult<AgencyDashboardDto>> GetAgencyDashboardAsync() =>
        await GetJsonAsync<AgencyDashboardDto>($"{_baseUrl}/agency/dashboard", "Sin acceso a panel de agencia.", showToast: false);

    public async Task<ApiResult<List<ReservationDto>>> GetAgencyReservationsAsync() =>
        await GetJsonAsync<List<ReservationDto>>($"{_baseUrl}/agency/reservations", "No se pudieron cargar reservas.", showToast: false);

    public async Task<ApiResult<bool>> SetReservationStatusAsync(int id, string status)
    {
        try
        {
            PrepareRequest();
            var response = await _http.PutAsync($"{_baseUrl}/agency/reservations/{id}/status?status={Uri.EscapeDataString(status)}", null);
            if (response.IsSuccessStatusCode) return ApiResult<bool>.Ok(true);
            return Fail<bool>("No se pudo actualizar la reserva.");
        }
        catch (Exception ex) { return Fail<bool>("Error de reserva.", ex); }
    }

    public async Task<ApiResult<VendorSalesDto>> CreateVendorAsync(CreateVendorRequest request)
    {
        try
        {
            PrepareRequest();
            var response = await _http.PostAsJsonAsync($"{_baseUrl}/agency/vendors", request);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<VendorSalesDto>>(JsonOptions);
            if (result?.Success == true && result.Data is not null)
                return ApiResult<VendorSalesDto>.Ok(result.Data);
            return Fail<VendorSalesDto>(result?.Message ?? "No se pudo crear vendedor.");
        }
        catch (Exception ex) { return Fail<VendorSalesDto>("Error al crear vendedor.", ex); }
    }

    public async Task<ApiResult<CreateDemoAgencyResponse>> CreateSuperAdminDemoAgencyAsync()
    {
        try
        {
            PrepareRequest();
            var response = await _http.PostAsync($"{_baseUrl}/superadmin/demo-agency", null);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<CreateDemoAgencyResponse>>(JsonOptions);
            if (result?.Success == true && result.Data is not null)
                return ApiResult<CreateDemoAgencyResponse>.Ok(result.Data);
            return Fail<CreateDemoAgencyResponse>(result?.Message ?? "No se pudo crear agencia demo.");
        }
        catch (Exception ex) { return Fail<CreateDemoAgencyResponse>("Error al crear agencia demo.", ex); }
    }

    public async Task<ApiResult<List<AgencyRankingDto>>> GetSuperAdminRankingAsync() =>
        await GetJsonAsync<List<AgencyRankingDto>>($"{_baseUrl}/superadmin/ranking", "Ranking no disponible.", showToast: false);

    public async Task<ApiResult<List<AgencyStaffDto>>> GetPartnerStaffAsync(int partnerId) =>
        await GetJsonAsync<List<AgencyStaffDto>>($"{_baseUrl}/superadmin/partners/{partnerId}/staff", "Equipo no disponible.", showToast: false);

    public async Task<ApiResult<bool>> UpdatePartnerAsync(int partnerId, UpdateAgencyRequest request)
    {
        try
        {
            PrepareRequest();
            var response = await _http.PutAsJsonAsync($"{_baseUrl}/superadmin/partners/{partnerId}", request);
            return response.IsSuccessStatusCode ? ApiResult<bool>.Ok(true) : Fail<bool>("No se pudo actualizar la agencia.");
        }
        catch (Exception ex) { return Fail<bool>("Error al actualizar agencia.", ex); }
    }

    public async Task<ApiResult<AgencyProfileDto>> UpdateAgencyProfileAsync(UpdateAgencyRequest request)
    {
        try
        {
            PrepareRequest();
            var response = await _http.PutAsJsonAsync($"{_baseUrl}/agency/profile", request);
            if (!response.IsSuccessStatusCode) return Fail<AgencyProfileDto>("No se pudo guardar el perfil.");
            return await GetAgencyProfileAsync();
        }
        catch (Exception ex) { return Fail<AgencyProfileDto>("Error de perfil.", ex); }
    }

    public async Task<ApiResult<List<AgencyTourListItemDto>>> GetAgencyToursAsync() =>
        await GetJsonAsync<List<AgencyTourListItemDto>>($"{_baseUrl}/agency/tours", "Tours no disponibles.", showToast: false);

    public async Task<ApiResult<bool>> UpdateTourItemAsync(
        int tourId, string? imageUrl = null, int? availableCapacity = null, int? busTotalSeats = null)
    {
        try
        {
            PrepareRequest();
            var response = await _http.PutAsJsonAsync($"{_baseUrl}/agency/tours/{tourId}",
                new { ImageUrl = imageUrl, AvailableCapacity = availableCapacity, BusTotalSeats = busTotalSeats });
            return response.IsSuccessStatusCode ? ApiResult<bool>.Ok(true) : Fail<bool>("No se pudo actualizar.");
        }
        catch (Exception ex) { return Fail<bool>("Error al actualizar tour.", ex); }
    }

    public async Task<ApiResult<bool>> UpdateTourDetailsAsync(int tourId, CreateTourRequest payload)
    {
        try
        {
            PrepareRequest();
            var response = await _http.PutAsJsonAsync($"{_baseUrl}/agency/tours/{tourId}", new
            {
                payload.Title,
                payload.Description,
                payload.Price,
                payload.Location,
                Department = payload.Department,
                payload.AdventureType,
                payload.ImageUrl,
                payload.PuntoPartida,
                payload.PuntoRetorno,
                payload.HoraSalida,
                payload.DuracionAproximada,
                payload.Itinerario,
                payload.QueIncluye,
                payload.QueNoIncluye,
                payload.QueLlevar,
                payload.Galeria
            });
            return response.IsSuccessStatusCode ? ApiResult<bool>.Ok(true) : Fail<bool>("No se pudo actualizar.");
        }
        catch (Exception ex) { return Fail<bool>("Error al actualizar tour.", ex); }
    }

    public async Task<ApiResult<bool>> DeleteTourAsync(int tourId)
    {
        try
        {
            PrepareRequest();
            var response = await _http.DeleteAsync($"{_baseUrl}/tours/{tourId}");
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                return Fail<bool>(string.IsNullOrWhiteSpace(body) ? "No se pudo eliminar el tour." : body, showToast: false);
            }
            return ApiResult<bool>.Ok(true);
        }
        catch (Exception ex) { return Fail<bool>("Error al eliminar tour.", ex); }
    }

    public async Task<ApiResult<bool>> DeleteAgencyPartnerAsync(int partnerId)
    {
        try
        {
            PrepareRequest();
            var response = await _http.DeleteAsync($"{_baseUrl}/agencies/{partnerId}");
            if (!response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ApiResponse<bool>>(JsonOptions);
                return Fail<bool>(result?.Message ?? "No se pudo eliminar la agencia.");
            }
            return ApiResult<bool>.Ok(true);
        }
        catch (Exception ex) { return Fail<bool>("Error al eliminar agencia.", ex); }
    }

    public async Task<ApiResult<List<TourCompareItemDto>>> GetCompareToursAsync(string? department = null)
    {
        var q = string.IsNullOrWhiteSpace(department) ? "" : $"?department={Uri.EscapeDataString(department)}";
        return await GetJsonAsync<List<TourCompareItemDto>>($"{_baseUrl}/superadmin/tours/compare{q}", "Comparación no disponible.", showToast: false);
    }

    public async Task<ApiResult<bool>> UpdateTourCapacityAsync(int tourId, int available)
    {
        try
        {
            PrepareRequest();
            var response = await _http.PutAsync($"{_baseUrl}/agency/tours/{tourId}/capacity?available={available}", null);
            return response.IsSuccessStatusCode ? ApiResult<bool>.Ok(true) : Fail<bool>("No se actualizaron cupos.");
        }
        catch (Exception ex) { return Fail<bool>("Error de inventario.", ex); }
    }

    public async Task<ApiResult<ManifestDto>> GetManifestAsync(int? tourId = null)
    {
        var q = tourId.HasValue ? $"?tourId={tourId}" : "";
        return await GetJsonAsync<ManifestDto>($"{_baseUrl}/agency/manifest{q}", "Manifiesto no disponible.", showToast: false);
    }

    public async Task<ApiResult<LoyaltyDto>> GetLoyaltyAsync() =>
        await GetJsonAsync<LoyaltyDto>($"{_baseUrl}/users/me/loyalty", "Puntos no disponibles.", showToast: false);

    public async Task<ApiResult<TourDto>> CreateAgencyTourAsync(CreateTourRequest request)
    {
        try
        {
            PrepareRequest();
            var response = await _http.PostAsJsonAsync($"{_baseUrl}/agency/tours", request);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<TourDto>>(JsonOptions);
            if (result?.Success == true && result.Data is not null)
                return ApiResult<TourDto>.Ok(result.Data);
            return Fail<TourDto>(result?.Message ?? "No se pudo crear el tour.");
        }
        catch (Exception ex) { return Fail<TourDto>("Error al crear tour.", ex); }
    }

    private async Task<ApiResult<T>> GetJsonAsync<T>(string url, string failMessage, bool showToast = true)
    {
        try
        {
            PrepareRequest();
            var response = await _http.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                var msg = (int)response.StatusCode is 401 or 403
                    ? "Sin acceso. Cierra sesión e ingresa de nuevo con tu cuenta."
                    : $"{failMessage} ({(int)response.StatusCode})";
                return Fail<T>(msg, showToast: showToast);
            }
            var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
            return data is not null ? ApiResult<T>.Ok(data) : Fail<T>("Sin datos", showToast: showToast);
        }
        catch (Exception ex) { return Fail<T>(failMessage, ex, showToast); }
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

    private ApiResult<T> Fail<T>(string message, Exception? ex = null, bool showToast = true)
    {
        var detail = ex is HttpRequestException ? " Verifica tu conexión con la API." : "";
        var full = message + detail;
        if (showToast) _toast.ShowError(full);
        return ApiResult<T>.Fail(full);
    }
}
