using System;
using System.ComponentModel.DataAnnotations;

namespace Vittal.DTO.UsuarioSala;

/// <summary>
/// Request DTO para asignar un doctor a una sala/área.
/// </summary>
public class UsuarioSalaRequestDto
{
    [Required(ErrorMessage = "El usuario es obligatorio")]
    public Guid UsuarioId { get; set; }

    [Required(ErrorMessage = "La sala es obligatoria")]
    public Guid SalaId { get; set; }
}

/// <summary>
/// Response DTO para lectura de asignaciones doctor-sala.
/// Incluye datos JOIN de Usuario y Sala para visualización.
/// </summary>
public class UsuarioSalaResponseDto
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }

    /// <summary>Nombre completo del usuario (Nombres + Apellidos)</summary>
    public string? UsuarioNombre { get; set; }

    public string? UsuarioEmail { get; set; }
    public Guid SalaId { get; set; }
    public string? SalaNombre { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
}
