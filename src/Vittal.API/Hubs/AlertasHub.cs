using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Vittal.API.Hubs;

/// <summary>
/// SignalR Hub para alertas en tiempo real.
/// Los clientes se suscriben al grupo de su clínica para recibir notificaciones de alertas.
/// Historia de Usuario: HU23 — Alertas Configurables
/// </summary>
[Authorize]
public class AlertasHub : Hub
{
    /// <summary>Suscribe al cliente al grupo de alertas de una clínica específica.</summary>
    /// <param name="clinicaId">ID de la clínica como string.</param>
    public async Task JoinGroup(string clinicaId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"clinica_{clinicaId}");
    }

    /// <summary>Desuscribe al cliente del grupo de alertas de una clínica.</summary>
    /// <param name="clinicaId">ID de la clínica como string.</param>
    public async Task LeaveGroup(string clinicaId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"clinica_{clinicaId}");
    }
}
