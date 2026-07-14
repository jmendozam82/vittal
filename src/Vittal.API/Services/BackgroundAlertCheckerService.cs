using Microsoft.AspNetCore.SignalR;
using Vittal.API.Hubs;
using Vittal.BLL.Interfaces;

namespace Vittal.API.Services;

/// <summary>
/// Servicio en segundo plano que verifica periódicamente los tiempos de espera
/// de los pacientes en todas las clínicas activas y genera alertas cuando
/// se excede el umbral configurado. Despacha notificaciones por SignalR.
///
/// Historia de Usuario: HU23 — Alertas Configurables
/// </summary>
public class BackgroundAlertCheckerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<AlertasHub> _hubContext;
    private readonly ILogger<BackgroundAlertCheckerService> _logger;

    /// <summary>Intervalo base de verificación (30 segundos).</summary>
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    public BackgroundAlertCheckerService(
        IServiceProvider serviceProvider,
        IHubContext<AlertasHub> hubContext,
        ILogger<BackgroundAlertCheckerService> logger)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BackgroundAlertCheckerService iniciado — intervalo: {Interval}s", CheckInterval.TotalSeconds);

        // Esperar a que la aplicación esté completamente iniciada
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await VerificarTodasLasClinicasAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Apagado graceful — ignorar
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el ciclo de verificación de alertas");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }

        _logger.LogInformation("BackgroundAlertCheckerService detenido.");
    }

    private async Task VerificarTodasLasClinicasAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var clinicaService = scope.ServiceProvider.GetRequiredService<IClinicaService>();
        var alertaService = scope.ServiceProvider.GetRequiredService<IAlertaEsperaService>();

        // Obtener todas las clínicas activas
        var clinicasResult = await clinicaService.GetAllAsync(incluirInactivos: false);
        if (!clinicasResult.IsSuccess || clinicasResult.Data == null)
        {
            _logger.LogWarning("No se pudieron obtener las clínicas para verificación de alertas.");
            return;
        }

        var clinicas = clinicasResult.Data.ToList();
        if (clinicas.Count == 0) return;

        _logger.LogDebug("Verificando tiempos de espera para {Count} clínicas.", clinicas.Count);

        foreach (var clinica in clinicas)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var result = await alertaService.VerificarTiemposEsperaAsync(clinica.Id);
                if (result.IsSuccess && result.Data > 0)
                {
                    // Despachar alertas por SignalR a los clientes de esta clínica
                    var noResueltas = await alertaService.GetNoResueltasAsync(clinica.Id);
                    if (noResueltas.IsSuccess && noResueltas.Data != null)
                    {
                        foreach (var alerta in noResueltas.Data)
                        {
                            await _hubContext.Clients
                                .Group($"clinica_{clinica.Id}")
                                .SendAsync("NuevaAlerta", alerta, ct);
                        }
                        _logger.LogInformation(
                            "Background: {Count} alerta(s) generada(s) y despachada(s) para clínica {ClinicaNombre} ({ClinicaId})",
                            result.Data, clinica.Nombre, clinica.Id);
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error verificando alertas para clínica {ClinicaId}", clinica.Id);
            }
        }
    }
}
