using DestinoPeruAPI.Domain.Enums;

namespace DestinoPeruAPI.Application.DTOs;

public record RegisterRequest(string Name, string Email, string Password, string Role = "Cliente");
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string Name, string Email, string Role, int UserId, int? PartnerId = null, bool Impersonating = false);

public record TourItineraryStepDto(string Hora, string Titulo, string Descripcion);

public record TourDto(
    int Id, int PartnerId, string PartnerName, string Slug, string Title, string Description,
    string MetaTitle, string MetaDescription, decimal Price, string Location, string Department,
    string AdventureType, DateTime Date, int Capacity, int AvailableCapacity,
    string? ImageUrl, bool IsActive, DateTime CreatedAt,
    string? PuntoPartida = null, string? PuntoRetorno = null, string? HoraSalida = null, string? DuracionAproximada = null,
    IReadOnlyList<TourItineraryStepDto>? Itinerario = null,
    IReadOnlyList<string>? QueIncluye = null, IReadOnlyList<string>? QueNoIncluye = null, IReadOnlyList<string>? QueLlevar = null,
    IReadOnlyList<string>? Galeria = null);

public record TourCompareItemDto(
    int Id, string Title, string PartnerName, string Department, string Location,
    decimal Price, string AdventureType, bool IsActive, DateTime Date);

public record TourContentInput(
    string? PuntoPartida = null, string? PuntoRetorno = null, string? HoraSalida = null, string? DuracionAproximada = null,
    IReadOnlyList<TourItineraryStepDto>? Itinerario = null,
    IReadOnlyList<string>? QueIncluye = null, IReadOnlyList<string>? QueNoIncluye = null,
    IReadOnlyList<string>? QueLlevar = null, IReadOnlyList<string>? Galeria = null);

public record TourSearchQuery(
    string? Department = null, string? Location = null, string? AdventureType = null,
    DateTime? FromDate = null, DateTime? ToDate = null, decimal? MaxPrice = null, int Page = 1, int PageSize = 12);

public record AgencyProfileDto(
    int PartnerId, string Name, string RUC, string? LogoUrl, string? OperatingDepartment,
    string? ContactEmail, string? ContactPhone, string Status, decimal CommissionRate);

public record CreateTourRequest(
    string Title, string Description, decimal Price, string Location, string Department,
    string AdventureType, DateTime Date, int Capacity, string? ImageUrl,
    string? MetaTitle = null, string? MetaDescription = null,
    string? PuntoPartida = null, string? PuntoRetorno = null, string? HoraSalida = null, string? DuracionAproximada = null,
    IReadOnlyList<TourItineraryStepDto>? Itinerario = null,
    IReadOnlyList<string>? QueIncluye = null, IReadOnlyList<string>? QueNoIncluye = null,
    IReadOnlyList<string>? QueLlevar = null, IReadOnlyList<string>? Galeria = null);

public record PartnerDto(
    int Id, int UserId, string UserName, string Name, string RUC, PartnerType PartnerType,
    string Status, string VerificationStatus, decimal CommissionRate, DateTime CreatedAt,
    int DocumentCount = 0,
    string? OperatingDepartment = null, string? LogoUrl = null,
    string? ContactEmail = null, string? ContactPhone = null, int StaffCount = 0);

public record CreatePartnerRequest(string Name, string RUC, PartnerType PartnerType = PartnerType.Agencia);

public record CreateAgencyRequest(
    string AgencyName, string RUC, string OperatingDepartment,
    string AdminName, string AdminEmail, string AdminPassword,
    PartnerType PartnerType = PartnerType.Agencia,
    string? LogoUrl = null, string? ContactEmail = null, string? ContactPhone = null);

public record CreateVendorRequest(string DisplayName, string Email, string Password);

public record SuperAdminMetricsDto(
    int TotalUsers, int TotalPartners, int PendingPartners, int TotalTours,
    int TotalReservations, decimal TotalRevenue, decimal TotalCommissions, int ActiveUsers);

public record AgencyRankingDto(int PartnerId, string Name, decimal Revenue, int Reservations, int TourCount);

public record UpdateAgencyRequest(
    string? Name, string? RUC, string? OperatingDepartment, string? LogoUrl,
    string? ContactEmail, string? ContactPhone, string? Status);

public record LoyaltyDto(int Points, int LifetimePoints);

public record AgencyStaffDto(int UserId, string Name, string Email, string DisplayName, string StaffRole);

public record ManifestDto(
    string TourTitle, DateTime TourDate, string AgencyName,
    IReadOnlyList<ManifestPassengerLineDto> Passengers);

public record ManifestPassengerLineDto(string FullName, string Dni, string PickupPoint, int Quantity);

public record AgencyTourListItemDto(
    int Id, string Title, string Slug, string Department, decimal Price,
    int AvailableCapacity, int Capacity, bool IsActive,
    string? ImageUrl = null, string AdventureType = "FullDay");

public record UpdateTourItemRequest(
    string? ImageUrl = null, int? AvailableCapacity = null, int? BusTotalSeats = null,
    string? Title = null, string? Description = null, decimal? Price = null,
    string? Location = null, string? Department = null, string? AdventureType = null,
    bool? IsActive = null,
    string? PuntoPartida = null, string? PuntoRetorno = null, string? HoraSalida = null, string? DuracionAproximada = null,
    IReadOnlyList<TourItineraryStepDto>? Itinerario = null,
    IReadOnlyList<string>? QueIncluye = null, IReadOnlyList<string>? QueNoIncluye = null,
    IReadOnlyList<string>? QueLlevar = null, IReadOnlyList<string>? Galeria = null);

public record CreateDemoAgencyResponse(
    int PartnerId, string AgencyName, string AdminEmail, string AdminPassword, int ToursCreated);

public record AgencyDashboardDto(
    int PartnerId, string PartnerName, int TotalTours, int PendingReservations,
    int ConfirmedReservations, decimal TotalRevenue, decimal AgencyCommissions,
    IReadOnlyList<VendorSalesDto> VendorSales);

public record VendorSalesDto(int UserId, string Name, int Reservations, decimal Revenue);

public record PartnerListItemDto(
    int Id, string Name, string RUC, string Status, string OperatingDepartment,
    string AdminEmail, string AdminName, int AdminUserId, int StaffCount, decimal Revenue,
    PartnerType PartnerType = PartnerType.Agencia, int ItemCount = 0);

public record CategoryMetricsDto(
    string Key, string Label, string Icon, int Partners, int ActiveItems,
    int Reservations, decimal Revenue, int PendingPartners);

public record SuperAdminDashboardDto(
    SuperAdminMetricsDto Global,
    IReadOnlyList<CategoryMetricsDto> Categories);
public record UploadDocumentRequest(DocumentType DocumentType, string FileUrl);

public record ReservationDto(
    int Id, int UserId, string UserName, int TourId, string TourTitle, string TourSlug,
    string TourLocation, DateTime TourDate, int Quantity, decimal Total, decimal Commission,
    int LoyaltyPointsEarned, string Status, DateTime CreatedAt);

public record CreateReservationRequest(int TourId, int Quantity, List<PassengerDto>? Passengers = null);
public record PassengerDto(string FullName, string Dni, string PickupPoint);

public record PaymentDto(
    int Id, int ReservationId, decimal Amount, string Method, string Status,
    string? TransactionId, string? VoucherUrl, string VoucherStatus, string? QrReference, DateTime CreatedAt);

public record CreatePaymentRequest(int ReservationId, string Method, string? QrReference = null);
public record SubmitVoucherRequest(int PaymentId, string VoucherUrl);

public record AdminMetricsDto(
    int TotalPartners, int PendingPartners, int TotalTours, int TotalReservations,
    decimal TotalCommissions, int ActiveUsers);

public record PopularDestinationDto(
    int Id, string Name, string ImageUrl, string Department, int DisplayOrder, bool IsActive);

public record UpsertPopularDestinationRequest(
    string Name, string ImageUrl, string? Department = null, int DisplayOrder = 0, bool IsActive = true);

public record ApiResponse<T>(bool Success, string? Message, T? Data);
