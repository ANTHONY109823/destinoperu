using DestinoPeruAPI.Domain.Enums;

namespace DestinoPeruAPI.Domain.Entities;

public class Partner
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Identificador legible para la URL pública /agencia/{slug}.</summary>
    public string? Slug { get; set; }

    public string RUC { get; set; } = string.Empty;
    public PartnerType PartnerType { get; set; } = PartnerType.Agencia;
    public string Status { get; set; } = "Pending";
    public string VerificationStatus { get; set; } = "Pending";
    public decimal CommissionRate { get; set; } = 0.10m;
    public string? OperatingDepartment { get; set; }
    public string? LogoUrl { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public User User { get; set; } = null!;
    public ICollection<PartnerStaff> Staff { get; set; } = new List<PartnerStaff>();
    public ICollection<Tour> Tours { get; set; } = new List<Tour>();
    public ICollection<PartnerDocument> Documents { get; set; } = new List<PartnerDocument>();
}
