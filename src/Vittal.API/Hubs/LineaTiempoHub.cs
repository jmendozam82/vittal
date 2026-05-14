using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Vittal.API.Hubs;

/// <summary>
/// SignalR Hub para línea de tiempo en tiempo real.
/// Los clientes se suscriben al grupo de su clínica para recibir actualizaciones de pasos de atención.
/// Historia de Usuario: HU19 — Línea de Tiempo
/// </summary>
[Authorize]
public class LineaTiempoHub : Hub
{
    /// <summary>Suscribe al cliente al grupo de timeline de una clínica específica.</summary>
    /// <param name="clinicaId">ID de la clínica como string.</param>
    public async Task SubscribeToClinica(string clinicaId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"timeline_{clinicaId}");
    }

    /// <summary>Desuscribe al cliente del grupo de timeline de una clínica.</summary>
    /// <param name="clinicaId">ID de la clínica como string.</param>
    public async Task UnsubscribeFromClinica(string clinicaId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"timeline_{clinicaId}");
    }
}
