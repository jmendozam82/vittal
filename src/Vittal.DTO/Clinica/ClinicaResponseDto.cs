namespace Vittal.DTO.Clinica;

public class ClinicaResponseDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? LogoUrl { get; set; }
    public int TiempoEsperaMinutos { get; set; }
    public string? BdExterna1 { get; set; }
    public string? BdExterna2 { get; set; }
    public string? HorarioApertura { get; set; }
    public string? HorarioCierre { get; set; }
    public string? DiasAtencion { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}
