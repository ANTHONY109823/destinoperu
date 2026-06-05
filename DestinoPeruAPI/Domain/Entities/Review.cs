namespace DestinoPeruAPI.Domain.Entities;

/// <summary>
/// Reseña verificada: solo la deja un cliente con una reserva pagada/confirmada del tour.
/// </summary>
public class Review
{
    public int Id { get; set; }
    public int TourId { get; set; }
    public int PartnerId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Tour Tour { get; set; } = null!;
    public User User { get; set; } = null!;
}
