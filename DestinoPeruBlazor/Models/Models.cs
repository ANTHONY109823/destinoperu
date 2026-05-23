namespace DestinoPeruBlazor.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}

public class AuthResponse
{
    public string Token { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public int UserId { get; set; }
}

public class TourDto
{
    public int Id { get; set; }
    public int AgencyId { get; set; }
    public string AgencyName { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public string Location { get; set; } = "";
    public DateTime Date { get; set; }
    public int Capacity { get; set; }
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
    public string TourLocation { get; set; } = "";
    public DateTime TourDate { get; set; }
    public int Quantity { get; set; }
    public decimal Total { get; set; }
    public decimal Commission { get; set; }
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
    public DateTime CreatedAt { get; set; }
}

public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public class RegisterRequest
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "Cliente";
}

public class CreateReservationRequest
{
    public int TourId { get; set; }
    public int Quantity { get; set; }
}

public class CreatePaymentRequest
{
    public int ReservationId { get; set; }
    public string Method { get; set; } = "Tarjeta";
}
