namespace Vittal.Entity;

/// <summary>
/// Clínicas registradas en el sistema. Cada clínica es un tenant del SaaS Vittal.
/// Tabla: public.clinicas
/// CASO ESPECIAL: Tabla raíz multi-tenant — NO tiene ClinicaId.
/// Historia de Usuario: HU09 — Gestión de Clínicas
/// </summary>
public class Clinica
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? LogoUrl { get; set; }
    public int TiempoEsperaMinutos { get; set; } = 30;
    public string? BdExterna1 { get; set; }
    public string? BdExterna2 { get; set; }
    public string? HorarioApertura { get; set; }
    public string? HorarioCierre { get; set; }
    public string? DiasAtencion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }
}
