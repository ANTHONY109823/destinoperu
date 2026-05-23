using DestinoPeruAPI.Domain.Enums;

namespace DestinoPeruAPI.Application.DTOs;

public record RegisterRequest(string Name, string Email, string Password, string Role = "Cliente");
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string Name, string Email, string Role, int UserId);

public record TourDto(
    int Id, int PartnerId, string PartnerName, string Slug, string Title, string Description,
    string MetaTitle, string MetaDescription, decimal Price, string Location, string Department,
    string AdventureType, DateTime Date, int Capacity, int AvailableCapacity,
    string? ImageUrl, bool IsActive, DateTime CreatedAt);

public record TourSearchQuery(
    string? Department = null, string? Location = null, string? AdventureType = null,
    DateTime? FromDate = null, decimal? MaxPrice = null, int Page = 1, int PageSize = 12);

public record CreateTourRequest(
    string Title, string Description, decimal Price, string Location, string Department,
    string AdventureType, DateTime Date, int Capacity, string? ImageUrl,
    string? MetaTitle = null, string? MetaDescription = null);

public record PartnerDto(
    int Id, int UserId, string UserName, string Name, string RUC, PartnerType PartnerType,
    string Status, string VerificationStatus, decimal CommissionRate, DateTime CreatedAt,
    int DocumentCount = 0);

public record CreatePartnerRequest(string Name, string RUC, PartnerType PartnerType = PartnerType.Agencia);
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

public record ApiResponse<T>(bool Success, string? Message, T? Data);
