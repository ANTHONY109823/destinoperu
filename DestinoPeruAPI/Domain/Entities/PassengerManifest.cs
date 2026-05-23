namespace DestinoPeruAPI.Domain.Entities;

public class PassengerManifest
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Dni { get; set; } = string.Empty;
    public string PickupPoint { get; set; } = string.Empty;
    public Reservation Reservation { get; set; } = null!;
}
