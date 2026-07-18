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

    // ── Monitoring counters ──────────────────────────────────────
    private int _totalExecutions;
    private int _alertasCreadas;
    private int _errores;
    private DateTime? _lastExecutionTime;
    private TimeSpan? _lastExecutionDuration;

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

        try
        {
            // Esperar a que la aplicación esté completamente iniciada
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                _totalExecutions++;

                try
                {
                    _logger.LogInformation(
                        "Ejecutando verificación de alertas #{Execution}", _totalExecutions);

                    var alertasEnCiclo = await VerificarTodasLasClinicasAsync(stoppingToken);
                    _alertasCreadas += alertasEnCiclo;

                    sw.Stop();
                    _lastExecutionTime = DateTime.UtcNow;
                    _lastExecutionDuration = sw.Elapsed;

                    _logger.LogInformation(
                        "Verificación #{Execution} completada en {Duration}ms. Alertas creadas: {AlertCount}",
                        _totalExecutions, sw.ElapsedMilliseconds, alertasEnCiclo);

                    // Log summary metrics every 10 executions
                    if (_totalExecutions % 10 == 0)
                    {
                        _logger.LogInformation(
                            "Métricas BackgroundAlertChecker: Total={Total}, Alertas={Alerts}, Errores={Errors}",
                            _totalExecutions, _alertasCreadas, _errores);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // Apagado graceful — ignorar
                    break;
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    _errores++;
                    _logger.LogError(ex,
                        "Error en verificación de alertas #{Execution} después de {Duration}ms",
                        _totalExecutions, sw.ElapsedMilliseconds);
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Apagado graceful — ignorar
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fatal en BackgroundAlertCheckerService. El servicio se detendrá.");
        }

        _logger.LogInformation("BackgroundAlertCheckerService detenido.");
    }

    private async Task<int> VerificarTodasLasClinicasAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var clinicaService = scope.ServiceProvider.GetRequiredService<IClinicaService>();
        var alertaService = scope.ServiceProvider.GetRequiredService<IAlertaEsperaService>();

        var totalAlertas = 0;

        // Obtener todas las clínicas activas
        var clinicasResult = await clinicaService.GetAllAsync(incluirInactivos: false);
        if (!clinicasResult.IsSuccess || clinicasResult.Data == null)
        {
            _logger.LogWarning("No se pudieron obtener las clínicas para verificación de alertas.");
            return 0;
        }

        var clinicas = clinicasResult.Data.ToList();
        if (clinicas.Count == 0) return 0;

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
                        totalAlertas += result.Data;
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

        return totalAlertas;
    }
}
