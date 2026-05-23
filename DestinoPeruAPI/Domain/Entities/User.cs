namespace DestinoPeruAPI.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "Cliente";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Partner? Partner { get; set; }
    public LoyaltyAccount? LoyaltyAccount { get; set; }
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
