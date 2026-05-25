namespace DestinoPeruBlazor.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

public class AuthResponse
{
    public string Token { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public int UserId { get; set; }
    public int? PartnerId { get; set; }
    public bool Impersonating { get; set; }
}

public class TourDto
{
    public int Id { get; set; }
    public int PartnerId { get; set; }
    public string PartnerName { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string MetaTitle { get; set; } = "";
    public string MetaDescription { get; set; } = "";
    public decimal Price { get; set; }
    public string Location { get; set; } = "";
    public string Department { get; set; } = "";
    public string AdventureType { get; set; } = "";
    public DateTime Date { get; set; }
    public int Capacity { get; set; }
    public int AvailableCapacity { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ReservationDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = "";
    public int TourId { get; set; }
    public string TourTitle { get; set; } = "";
    public string TourSlug { get; set; } = "";
    public string TourLocation { get; set; } = "";
    public DateTime TourDate { get; set; }
    public int Quantity { get; set; }
    public decimal Total { get; set; }
    public decimal Commission { get; set; }
    public int LoyaltyPointsEarned { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class PaymentDto
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "";
    public string Status { get; set; } = "";
    public string? TransactionId { get; set; }
    public string? VoucherUrl { get; set; }
    public string VoucherStatus { get; set; } = "";
    public string? QrReference { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminMetricsDto
{
    public int TotalPartners { get; set; }
    public int PendingPartners { get; set; }
    public int TotalTours { get; set; }
    public int TotalReservations { get; set; }
    public decimal TotalCommissions { get; set; }
    public int ActiveUsers { get; set; }
}

public class SuperAdminMetricsDto
{
    public int TotalUsers { get; set; }
    public int TotalPartners { get; set; }
    public int PendingPartners { get; set; }
    public int TotalTours { get; set; }
    public int TotalReservations { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalCommissions { get; set; }
    public int ActiveUsers { get; set; }
}

public class PartnerListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string RUC { get; set; } = "";
    public string Status { get; set; } = "";
    public string OperatingDepartment { get; set; } = "";
    public string AdminEmail { get; set; } = "";
    public string AdminName { get; set; } = "";
    public int AdminUserId { get; set; }
    public int StaffCount { get; set; }
    public decimal Revenue { get; set; }
    public int PartnerType { get; set; }
    public int ItemCount { get; set; }
}

public class CategoryMetricsDto
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public string Icon { get; set; } = "";
    public int Partners { get; set; }
    public int ActiveItems { get; set; }
    public int Reservations { get; set; }
    public decimal Revenue { get; set; }
    public int PendingPartners { get; set; }
}

public class SuperAdminDashboardDto
{
    public SuperAdminMetricsDto Global { get; set; } = new();
    public List<CategoryMetricsDto> Categories { get; set; } = [];
}

public class AgencyDashboardDto
{
    public int PartnerId { get; set; }
    public string PartnerName { get; set; } = "";
    public int TotalTours { get; set; }
    public int PendingReservations { get; set; }
    public int ConfirmedReservations { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AgencyCommissions { get; set; }
    public List<VendorSalesDto> VendorSales { get; set; } = [];
}

public class VendorSalesDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public int Reservations { get; set; }
    public decimal Revenue { get; set; }
}

public class CreateAgencyRequest
{
    public string AgencyName { get; set; } = "";
    public string RUC { get; set; } = "";
    public string OperatingDepartment { get; set; } = "";
    public int PartnerType { get; set; }
    public string AdminName { get; set; } = "";
    public string AdminEmail { get; set; } = "";
    public string AdminPassword { get; set; } = "";
    public string? LogoUrl { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
}

public class CreateVendorRequest
{
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class AgencyTourListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Slug { get; set; } = "";
    public string Department { get; set; } = "";
    public decimal Price { get; set; }
    public int AvailableCapacity { get; set; }
    public int Capacity { get; set; }
    public bool IsActive { get; set; }
    public string UnitLabel { get; set; } = "cupos";
}

public class AgencyProfileDto
{
    public int PartnerId { get; set; }
    public string Name { get; set; } = "";
    public string RUC { get; set; } = "";
    public string? LogoUrl { get; set; }
    public string? OperatingDepartment { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string Status { get; set; } = "";
    public decimal CommissionRate { get; set; }
}

public class CreateTourRequest
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public string Location { get; set; } = "";
    public string Department { get; set; } = "";
    public string AdventureType { get; set; } = "";
    public DateTime Date { get; set; }
    public int Capacity { get; set; }
    public string? ImageUrl { get; set; }
}

public class AgencyRankingDto
{
    public int PartnerId { get; set; }
    public string Name { get; set; } = "";
    public decimal Revenue { get; set; }
    public int Reservations { get; set; }
    public int TourCount { get; set; }
}

public class UpdateAgencyRequest
{
    public string? Name { get; set; }
    public string? RUC { get; set; }
    public string? OperatingDepartment { get; set; }
    public string? LogoUrl { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Status { get; set; }
}

public class AgencyStaffDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string StaffRole { get; set; } = "";
}

public class ManifestDto
{
    public string TourTitle { get; set; } = "";
    public DateTime TourDate { get; set; }
    public string AgencyName { get; set; } = "";
    public List<ManifestPassengerLineDto> Passengers { get; set; } = [];
}

public class ManifestPassengerLineDto
{
    public string FullName { get; set; } = "";
    public string Dni { get; set; } = "";
    public string PickupPoint { get; set; } = "";
    public int Quantity { get; set; }
}

public class LoyaltyDto
{
    public int Points { get; set; }
    public int LifetimePoints { get; set; }
}

public class CartItem
{
    public int TourId { get; set; }
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public string Department { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; } = 1;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

public class LoginRequest { public string Email { get; set; } = ""; public string Password { get; set; } = ""; }
public class RegisterRequest { public string Name { get; set; } = ""; public string Email { get; set; } = ""; public string Password { get; set; } = ""; public string Role { get; set; } = "Cliente"; }
public class CreateReservationRequest { public int TourId { get; set; } public int Quantity { get; set; } public List<PassengerDto>? Passengers { get; set; } }
public class PassengerDto { public string FullName { get; set; } = ""; public string Dni { get; set; } = ""; public string PickupPoint { get; set; } = ""; }
public class CreatePaymentRequest { public int ReservationId { get; set; } public string Method { get; set; } = "Tarjeta"; public string? QrReference { get; set; } }
public class SubmitVoucherRequest { public int PaymentId { get; set; } public string VoucherUrl { get; set; } = ""; }
