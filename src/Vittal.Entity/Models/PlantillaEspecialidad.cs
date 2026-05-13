namespace Vittal.Entity.Models;

/// <summary>
/// Catálogo global de especialidades médicas del sistema.
/// Tabla: public.plantillas_especialidad
/// CASO ESPECIAL: Tabla raíz global del sistema — NO tiene ClinicaId.
/// Historia de Usuario: HU-E02 — Plantillas de Especialidad
/// </summary>
public class PlantillaEspecialidad
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Icono { get; set; }
    
    // Auditoría base
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
    
    // Relaciones
    public ICollection<PlantillaItem> Items { get; set; } = new List<PlantillaItem>();
}
