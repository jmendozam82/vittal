using System.ComponentModel.DataAnnotations;

namespace Vittal.DTO.SignosVitalesHoja;

/// <summary>
/// Request DTO para crear o editar un registro de signo vital en una hoja de cita.
/// No expone campos de auditoría ni tenant — el servidor los maneja automáticamente.
/// Historia de Usuario: HU-E06 — Signos Vitales por Consulta
/// </summary>
public class SignosVitalesHojaRequestDto
{
    /// <summary>Hoja de cita a la que pertenece (obligatorio).</summary>
    [Required(ErrorMessage = "La hoja de cita es obligatoria.")]
    public Guid HojaCitaId { get; set; }

    /// <summary>Sala donde se tomó el signo vital (obligatorio).</summary>
    [Required(ErrorMessage = "La sala es obligatoria.")]
    public Guid SalaId { get; set; }

    /// <summary>Tipo de signo vital del catálogo por sala (obligatorio).</summary>
    [Required(ErrorMessage = "El tipo de signo vital es obligatorio.")]
    public Guid TipoSignoVitalId { get; set; }

    /// <summary>Valor numérico del signo vital (obligatorio).</summary>
    [Required(ErrorMessage = "El valor es obligatorio.")]
    [Range(0, 999999.99, ErrorMessage = "El valor debe estar entre 0 y 999,999.99.")]
    public decimal Valor { get; set; }

    /// <summary>Unidad de medida del valor registrado.</summary>
    [StringLength(20, ErrorMessage = "La unidad no puede exceder 20 caracteres.")]
    public string? Unidad { get; set; }

    /// <summary>Fecha y hora en que se tomó el signo vital. Si no se especifica, se usa la hora actual.</summary>
    public DateTime? FechaHora { get; set; }

    /// <summary>Usuario que registró el signo vital.</summary>
    public Guid? RegistradoPor { get; set; }
}
