using System.ComponentModel.DataAnnotations;

namespace Vittal.DTO.ConfiguracionAlerta;

/// <summary>
/// Request DTO para crear o actualizar la configuración de alertas de una clínica.
/// Historia de Usuario: HU23 — Alertas Configurables
/// </summary>
public class ConfiguracionAlertaRequestDto
{
    /// <summary>Tiempo máximo de espera en minutos antes de disparar una alerta.</summary>
    [Required(ErrorMessage = "El tiempo máximo de espera es obligatorio.")]
    [Range(1, 999, ErrorMessage = "El tiempo de espera debe estar entre 1 y 999 minutos.")]
    public int TiempoEsperaMaximoMinutos { get; set; }

    /// <summary>Indica si las alertas están habilitadas (true) o deshabilitadas (false).</summary>
    public bool Activo { get; set; } = true;

    /// <summary>Indica si la notificación debe reproducir un sonido.</summary>
    public bool NotificacionSonido { get; set; } = true;

    /// <summary>Intervalo en segundos entre revisiones de tiempos de espera.</summary>
    [Range(10, 300, ErrorMessage = "El intervalo de revisión debe estar entre 10 y 300 segundos.")]
    public int IntervaloRevisionSegundos { get; set; } = 30;
}
