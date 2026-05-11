using System;

namespace Vittal.DTO.Medicamento;

/// <summary>
/// Response DTO para datos del medicamento.
/// Incluye nombre completo calculado.
/// Historia de Usuario: HU08 — Gestión de Medicamentos
/// </summary>
public class MedicamentoResponseDto
{
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Concentracion { get; set; }
    public string? UnidadMedida { get; set; }

    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }

    /// <summary>Nombre + concentración concatenados.</summary>
    public string NombreCompleto { get; set; } = string.Empty;
}
