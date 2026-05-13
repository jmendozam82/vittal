namespace Vittal.Entity.Models;

/// <summary>
/// Hoja de cita médica que vincula una cita (Cita) con los datos clínicos
/// registrados durante la consulta (signos vitales, diagnósticos, tratamientos, etc.).
/// Tabla: public.hojas_cita
/// </summary>
public class HojaCita
{
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid CitaId { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
}
