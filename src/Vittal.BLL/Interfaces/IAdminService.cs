using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vittal.DTO.Clinica;
using Vittal.DTO.Usuario;
using Vittal.Utility.Results;

namespace Vittal.BLL.Interfaces;

/// <summary>
/// Servicio de administración global del sistema Vittal.
/// Solo accesible por usuarios con es_super_admin = true.
/// Historias de Usuario: HU-PC01 — Provisionamiento, HU-AD01 — Admin Global
/// </summary>
public interface IAdminService
{
    /// <summary>
    /// Crea una nueva clínica con provisionamiento completo:
    /// clínica + perfil admin + permisos + usuario Supabase Auth + config por defecto.
    /// </summary>
    Task<ServiceResult<ClinicaProvisionResponseDto>> ProvisionClinicaAsync(
        ClinicaProvisionRequestDto dto, Guid superAdminUsuarioId);

    /// <summary>
    /// Obtiene los usuarios de una clínica específica (Super Admin).
    /// </summary>
    Task<ServiceResult<IEnumerable<UsuarioResponseDto>>> GetUsuariosByClinicaAsync(
        Guid clinicaId, bool incluirInactivos = false);
}
