namespace DestinoPeruAPI.Domain.Entities;

public class AppMaintenanceRun
{
    public string Key { get; set; } = string.Empty;
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}
