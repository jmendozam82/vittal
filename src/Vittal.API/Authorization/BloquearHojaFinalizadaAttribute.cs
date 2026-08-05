using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Vittal.API.Extensions;
using Vittal.API.Helpers;
using Vittal.API.Models;
using Vittal.BLL.Interfaces;

namespace Vittal.API.Authorization;

/// <summary>
/// Filtro de integridad clínica: bloquea endpoints de escritura de items de hoja de cita
/// cuando la consulta asociada ya fue finalizada (estado de cita = 'atendida').
/// Extrae <c>HojaCitaId</c> del argumento de acción (DTO del body o parámetro de formulario)
/// y devuelve 400 BadRequest si la consulta está finalizada.
/// Aplicar SOLO a operaciones de escritura de contenido clínico (Create/Update/Upload).
/// La emisión de Constancias y los endpoints de lectura NO deben usar este filtro.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class BloquearHojaFinalizadaAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var hojaCitaId = ExtraerHojaCitaId(context.ActionArguments);

        // Sin hoja de cita identificada: no aplica bloqueo.
        if (!hojaCitaId.HasValue || hojaCitaId.Value == Guid.Empty)
        {
            await next();
            return;
        }

        var clinicaId = context.HttpContext.User.GetClinicaId();
        var hojaCitaService = context.HttpContext.RequestServices.GetRequiredService<IHojaCitaService>();

        var bloqueo = await ConsultaFinalizadaGuard.ValidateAsync(clinicaId, hojaCitaId, hojaCitaService);
        if (bloqueo != null)
        {
            context.Result = bloqueo;
            return;
        }

        await next();
    }

    /// <summary>
    /// Localiza el <c>HojaCitaId</c> en los argumentos de la acción.
    /// Soporta: DTO con propiedad <c>HojaCitaId</c> (Guid o Guid?) y parámetro directo
    /// de formulario/route llamado <c>hojaCitaId</c> (Guid?).
    /// </summary>
    private static Guid? ExtraerHojaCitaId(IDictionary<string, object?> actionArguments)
    {
        foreach (var kvp in actionArguments)
        {
            // Caso 2: parámetro directo con nombre hojaCitaId (form/route/query).
            // Un Guid? con valor se desempaqueta (boxea) como Guid, y null como null.
            if (string.Equals(kvp.Key, "hojaCitaId", StringComparison.OrdinalIgnoreCase))
            {
                if (kvp.Value is Guid directo)
                {
                    return directo;
                }
            }

            // Caso 1: DTO bindeado con propiedad HojaCitaId.
            if (kvp.Value == null)
            {
                continue;
            }

            var prop = kvp.Value.GetType().GetProperty("HojaCitaId",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

            if (prop == null)
            {
                continue;
            }

            var val = prop.GetValue(kvp.Value);
            if (val is Guid dtoGuid && dtoGuid != Guid.Empty)
            {
                return dtoGuid;
            }
        }

        return null;
    }
}
