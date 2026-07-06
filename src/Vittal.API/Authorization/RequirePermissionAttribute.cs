using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Vittal.API.Extensions;
using Vittal.BLL.Services;
using Vittal.BLL.Interfaces;
using Vittal.Utility;

namespace Vittal.API.Authorization;

    /// <summary>
    /// Attribute que verifica permisos antes de ejecutar un endpoint.
    /// Los Super Admins globales siempre tienen acceso.
    /// Los administradores de clínica tienen acceso a todos los módulos
    /// EXCEPTO aquellos que son exclusivos del Super Admin ("permisos", "clinicas").
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class RequirePermissionAttribute : Attribute, IAuthorizationFilter
    {
        // Módulos exclusivos del Super Admin — los administradores de clínica NO pueden gestionarlos
        private static readonly HashSet<string> SuperAdminModules = new(StringComparer.OrdinalIgnoreCase)
        {
            "permisos",
            "clinicas",
            "plantillas_especialidad"
        };

        private readonly string _module;
        private readonly PermissionType _permissionType;

        public RequirePermissionAttribute(string module, PermissionType permissionType)
        {
            _module = module;
            _permissionType = permissionType;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            // Super Admin bypass: los Super Admins Globales SIEMPRE tienen acceso
            if (user.EsSuperAdmin())
            {
                return;
            }

            // Admin bypass: los administradores de clínica tienen acceso a TODO
            // EXCEPTO módulos exclusivos del Super Admin (permisos, clinicas)
            if (user.EsAdmin() && !SuperAdminModules.Contains(_module))
            {
                return;
            }

            // Usuario no autenticado
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // Obtener IDs de claims
            var clinicaId = user.GetClinicaId();
            var perfilId = user.GetInternalPerfilId();

            if (clinicaId == Guid.Empty || perfilId == Guid.Empty)
            {
                context.Result = new ForbidResult();
                return;
            }

            // Resolver IPermisoService del contenedor DI
            var permisoService = context.HttpContext.RequestServices
                .GetRequiredService<IPermisoService>();

            // Verificar permiso de forma síncrona
            var task = permisoService.HasPermissionAsync(clinicaId, perfilId, _module, _permissionType);
            task.Wait(); // Necesario porque IAuthorizationFilter es síncrono
            var result = task.Result;

            if (!result.IsSuccess || !result.Data)
            {
                context.Result = new ForbidResult();
            }
        }
    }
