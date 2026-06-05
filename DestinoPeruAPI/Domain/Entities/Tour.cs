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
    public string? PuntoPartida { get; set; }
    public string? PuntoRetorno { get; set; }
    public string? HoraSalida { get; set; }
    public string? DuracionAproximada { get; set; }
    public string? ItinerarioJson { get; set; }
    public string? QueIncluyeJson { get; set; }
    public string? QueNoIncluyeJson { get; set; }
    public string? QueLlevarJson { get; set; }
    public string? GaleriaJson { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public uint RowVersion { get; set; }
    public Partner Partner { get; set; } = null!;
    public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
