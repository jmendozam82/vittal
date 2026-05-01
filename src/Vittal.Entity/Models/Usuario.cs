using System;

namespace Vittal.Entity.Models;

public class Usuario
{
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid PerfilId { get; set; }
    public Guid AuthUserId { get; set; }
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaActualizacion { get; set; }

    // Propiedades adicionales (se llenan via Dapper join o calculadas)
    public bool EsAdmin { get; set; } 
    public string NombreCompleto => $"{Nombres} {Apellidos}";
    public string PerfilNombre { get; set; } = string.Empty;
}
