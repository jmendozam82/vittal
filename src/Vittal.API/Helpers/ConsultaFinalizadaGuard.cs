using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Vittal.API.Models;
using Vittal.BLL.Interfaces;

namespace Vittal.API.Helpers;

/// <summary>
/// Guard de integridad clínica: bloquea la modificación de items de una hoja de cita
/// cuando la consulta asociada ya fue finalizada (estado de cita = 'atendida').
/// Devuelve <c>null</c> cuando la edición está permitida, o un <see cref="BadRequestObjectResult"/>
/// con el mensaje estándar cuando la consulta ya está finalizada.
/// La emisión de Constancias NO pasa por este guard (se permite en consultas finalizadas).
/// </summary>
public static class ConsultaFinalizadaGuard
{
    /// <summary>Mensaje estándar de bloqueo para consultas finalizadas.</summary>
    public const string MensajeBloqueo = "La consulta ya fue finalizada y no se puede modificar.";

    /// <summary>
    /// Valida que la hoja de cita no pertenezca a una consulta finalizada.
    /// Retorna un IActionResult de error (400) si está finalizada; <c>null</c> si puede continuar.
    /// </summary>
    public static async Task<IActionResult?> ValidateAsync(
        Guid clinicaId,
        Guid? hojaCitaId,
        IHojaCitaService hojaCitaService)
    {
        // Sin hoja de cita asociada (p.ej. archivo sin hoja): no hay restricción.
        if (!hojaCitaId.HasValue || hojaCitaId.Value == Guid.Empty)
        {
            return null;
        }

        var finalizada = await hojaCitaService.EstaFinalizadaAsync(clinicaId, hojaCitaId.Value);
        if (!finalizada)
        {
            return null;
        }

        return new BadRequestObjectResult(new ApiResponse<object>
        {
            Success = false,
            Message = MensajeBloqueo,
            Errors = { "La consulta médica asociada a esta hoja ya fue finalizada y su contenido está bloqueado para edición." }
        });
    }
}
