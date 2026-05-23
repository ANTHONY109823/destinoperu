namespace DestinoPeruAPI.Domain.Entities;

public class LoyaltyAccount
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int Points { get; set; }
    public int LifetimePoints { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public User User { get; set; } = null!;
}
