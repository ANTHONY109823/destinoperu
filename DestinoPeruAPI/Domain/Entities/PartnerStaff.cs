namespace DestinoPeruAPI.Domain.Entities;

public class PartnerStaff
{
    public int Id { get; set; }
    public int PartnerId { get; set; }
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string StaffRole { get; set; } = "Vendedor";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Partner Partner { get; set; } = null!;
    public User User { get; set; } = null!;
}
