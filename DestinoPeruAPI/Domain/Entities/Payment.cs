namespace DestinoPeruAPI.Domain.Entities;

public class Payment
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? TransactionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Reservation Reservation { get; set; } = null!;
}
