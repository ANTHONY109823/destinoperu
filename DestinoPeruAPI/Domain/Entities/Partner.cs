using DestinoPeruAPI.Domain.Enums;

namespace DestinoPeruAPI.Domain.Entities;

public class Partner
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RUC { get; set; } = string.Empty;
    public PartnerType PartnerType { get; set; } = PartnerType.Agencia;
    public string Status { get; set; } = "Pending";
    public string VerificationStatus { get; set; } = "Pending";
    public decimal CommissionRate { get; set; } = 0.10m;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User User { get; set; } = null!;
    public ICollection<Tour> Tours { get; set; } = new List<Tour>();
    public ICollection<PartnerDocument> Documents { get; set; } = new List<PartnerDocument>();
}
