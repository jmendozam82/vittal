namespace Vittal.DTO.Clinica;

/// <summary>
/// DTO de respuesta para la operación de provisionamiento completo de una clínica.
/// Retorna los IDs de todos los recursos creados.
/// Historia de Usuario: HU-PC01 — Provisionamiento Automático de Clínica
/// </summary>
public class ClinicaProvisionResponseDto
{
    /// <summary>ID de la clínica creada.</summary>
    public Guid ClinicaId { get; set; }

    /// <summary>Nombre de la clínica creada.</summary>
    public string ClinicaNombre { get; set; } = string.Empty;

    /// <summary>ID del perfil administrador creado.</summary>
    public Guid PerfilAdminId { get; set; }

    /// <summary>Nombre del perfil administrador.</summary>
    public string PerfilAdminNombre { get; set; } = string.Empty;

    /// <summary>ID del usuario administrador creado (tabla usuarios local).</summary>
    public Guid AdminUsuarioId { get; set; }

    /// <summary>ID del usuario en Supabase Auth.</summary>
    public Guid? AdminAuthUserId { get; set; }

    /// <summary>Nombre completo del administrador.</summary>
    public string AdminNombreCompleto { get; set; } = string.Empty;

    /// <summary>Email del administrador.</summary>
    public string AdminEmail { get; set; } = string.Empty;

    /// <summary>Username del administrador.</summary>
    public string AdminUsername { get; set; } = string.Empty;

    /// <summary>Cantidad de permisos seedeados para el perfil admin.</summary>
    public int PermisosSeedeados { get; set; }

    /// <summary>Indica si se creó la configuración de alertas por defecto.</summary>
    public bool ConfigAlertasCreada { get; set; }

    /// <summary>Indica si se creó la configuración de dashboard por defecto.</summary>
    public bool DashboardConfigCreada { get; set; }

    /// <summary>Timestamp de creación.</summary>
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
