using System;

namespace Vittal.DTO.Paciente;

/// <summary>

/// Response DTO para datos del paciente.

/// Incluye nombre del doctor y nombre completo calculado.

/// Historia de Usuario: HU07 — Gestión de Pacientes

/// </summary>

public class PacienteResponseDto

{
    public Guid Id { get; set; }

    public Guid ClinicaId { get; set; }

    public Guid DoctorId { get; set; }

    public string DoctorNombre { get; set; } = string.Empty;

    public string PrimerNombre { get; set; } = string.Empty;

    public string? SegundoNombre { get; set; }

    public string PrimerApellido { get; set; } = string.Empty;

    public string? SegundoApellido { get; set; }

    public string? Email { get; set; }

    public string? Celular { get; set; }

    public string? Direccion { get; set; }

    public string? Sexo { get; set; }

    public DateOnly? FechaNacimiento { get; set; }

    public string? FotoUrl { get; set; }

    public string? TipoDocumentoIdentificacion { get; set; }

    public string? NumeroDocumentoIdentificacion { get; set; }

    public string? Observaciones { get; set; }

    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaModificacion { get; set; }

    /// <summary>Nombre completo del paciente (calculado por la entidad).</summary>

    public string NombreCompleto { get; set; } = string.Empty;
}
