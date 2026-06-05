namespace DestinoPeruAPI.Domain.Entities;

/// <summary>
/// Destino popular gestionable desde el panel Super Admin.
/// Aparece en la Home pública y al hacer clic filtra el catálogo por <see cref="Department"/>.
/// </summary>
public class PopularDestination
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>Departamento por el que se filtra el catálogo al pulsar la tarjeta.</summary>
    public string Department { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
