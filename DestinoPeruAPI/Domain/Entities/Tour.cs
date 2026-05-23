namespace DestinoPeruAPI.Domain.Entities;

public class Tour
{
    public int Id { get; set; }
    public int PartnerId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MetaTitle { get; set; } = string.Empty;
    public string MetaDescription { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string AdventureType { get; set; } = "Aventura";
    public DateTime Date { get; set; }
    public int Capacity { get; set; }
    public int AvailableCapacity { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Partner Partner { get; set; } = null!;
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
