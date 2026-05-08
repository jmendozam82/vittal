using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vittal.DAL.Repositories;
using Vittal.DTO.Permiso;
using Vittal.Utility;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Implementación del servicio de verificación y gestión de permisos.
/// Delega en IPermisoRepository para consultar la base de datos.
/// </summary>
public class PermisoService : IPermisoService
{
    private readonly IPermisoRepository _permisoRepository;
    private readonly ILogger<PermisoService> _logger;

    public PermisoService(IPermisoRepository permisoRepository, ILogger<PermisoService> logger)
    {
        _permisoRepository = permisoRepository;
        _logger = logger;
    }

    public async Task<ServiceResult<bool>> HasPermissionAsync(
        Guid clinicaId, Guid perfilId, string moduloClave, PermissionType tipoPermiso)
    {
        try
        {
            var (puedeLeer, puedeCrear, puedeActualizar) = await _permisoRepository.GetPermisosAsync(
                clinicaId, perfilId, moduloClave);

            bool tienePermiso = tipoPermiso switch
            {
                PermissionType.Read => puedeLeer,
                PermissionType.Create => puedeCrear,
                PermissionType.Update => puedeActualizar,
                _ => false
            };

            return ServiceResult<bool>.Success(tienePermiso);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar permiso {Tipo} para módulo {Modulo}", 
                tipoPermiso, moduloClave);
            return ServiceResult<bool>.Failure("Error al verificar permisos.", ServiceErrorType.InternalError);
        }
    }

    public async Task<ServiceResult<List<PermisoResponseDto>>> GetPermisosByPerfilAsync(
        Guid clinicaId, Guid perfilId)
    {
        try
        {
            var resultados = await _permisoRepository.GetPermisosByPerfilAsync(clinicaId, perfilId);

            var dtos = resultados.Select(r => new PermisoResponseDto
            {
                Id = r.permisoid,
                ModuloId = r.moduloid,
                ModuloClave = r.clave,
                ModuloNombre = r.nombre,
                ModuloDescripcion = r.descripcion,
                PuedeLeer = r.puedeLeer,
                PuedeCrear = r.puedeCrear,
                PuedeActualizar = r.puedeActualizar
            }).ToList();

            return ServiceResult<List<PermisoResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener permisos del perfil {PerfilId}", perfilId);
            return ServiceResult<List<PermisoResponseDto>>.Failure(
                "Error al obtener permisos del perfil.", ServiceErrorType.InternalError);
        }
    }

    public async Task<ServiceResult<bool>> UpdatePermisosAsync(
        Guid clinicaId, Guid perfilId, PermisoUpdateRequestDto request, Guid usuarioId)
    {
        try
        {
            int actualizados = 0;
            foreach (var item in request.Permisos)
            {
                var ok = await _permisoRepository.UpsertPermisoAsync(
                    clinicaId, perfilId, item.ModuloId,
                    item.PuedeLeer, item.PuedeCrear, item.PuedeActualizar,
                    usuarioId);
                if (ok) actualizados++;
            }

            _logger.LogInformation("Permisos actualizados para perfil {PerfilId}: {Count} módulos",
                perfilId, actualizados);

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar permisos del perfil {PerfilId}", perfilId);
            return ServiceResult<bool>.Failure(
                "Error al actualizar permisos.", ServiceErrorType.InternalError);
        }
    }
}
