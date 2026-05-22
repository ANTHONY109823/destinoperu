namespace DestinoPeruAPI.Domain.Entities;

public class Reservation
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int TourId { get; set; }
    public int Quantity { get; set; }
    public decimal Total { get; set; }
    public decimal Commission { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User User { get; set; } = null!;
    public Tour Tour { get; set; } = null!;
    public Payment? Payment { get; set; }
}
