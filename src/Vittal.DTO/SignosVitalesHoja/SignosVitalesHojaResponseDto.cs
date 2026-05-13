using System;

namespace Vittal.DTO.SignosVitalesHoja;

/// <summary>
/// Response DTO para datos del signo vital registrado en una hoja de cita.
/// Incluye nombres descriptivos de sala, tipo de signo vital y registrador.
/// Historia de Usuario: HU-E06 — Signos Vitales por Consulta
/// </summary>
public class SignosVitalesHojaResponseDto
{
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid HojaCitaId { get; set; }
    public Guid SalaId { get; set; }

    /// <summary>Nombre de la sala (para mostrar en UI).</summary>
    public string SalaNombre { get; set; } = string.Empty;

    public Guid TipoSignoVitalId { get; set; }

    /// <summary>Nombre del tipo de signo vital (para mostrar en UI).</summary>
    public string TipoSignoVitalNombre { get; set; } = string.Empty;

    public decimal Valor { get; set; }
    public string? Unidad { get; set; }

    /// <summary>Indica si el valor está fuera del rango normal.</summary>
    public bool FueraDeRango { get; set; }

    public DateTime FechaHora { get; set; }

    /// <summary>ID del usuario que registró el signo vital.</summary>
    public Guid? RegistradoPor { get; set; }

    /// <summary>Nombre completo del usuario que registró (para mostrar en UI).</summary>
    public string? RegistradoPorNombre { get; set; }

    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
}
