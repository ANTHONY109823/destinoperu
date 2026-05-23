using DestinoPeruAPI.Domain.Enums;

namespace DestinoPeruAPI.Domain.Entities;

public class PartnerDocument
{
    public int Id { get; set; }
    public int PartnerId { get; set; }
    public DocumentType DocumentType { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Partner Partner { get; set; } = null!;
}
