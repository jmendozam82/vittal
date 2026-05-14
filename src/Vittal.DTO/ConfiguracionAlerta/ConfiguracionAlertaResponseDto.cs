using System;

namespace Vittal.DTO.ConfiguracionAlerta;

/// <summary>
/// Response DTO para la configuración de alertas de una clínica.
/// Historia de Usuario: HU23 — Alertas Configurables
/// </summary>
public class ConfiguracionAlertaResponseDto
{
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public int TiempoEsperaMaximoMinutos { get; set; }
    public bool Activo { get; set; }
    public bool NotificacionSonido { get; set; }
    public int IntervaloRevisionSegundos { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public Guid? CreadoPor { get; set; }
    public Guid? ModificadoPor { get; set; }
}
