using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vittal.BLL.Interfaces;
using Vittal.DAL.Interfaces;
using Vittal.DTO.LineaTiempo;
using Vittal.Entity;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de línea de tiempo de atención de citas.
/// Gestiona los pasos secuenciales del paciente durante su consulta.
/// Historia de Usuario: HU19 — Línea de Tiempo
/// </summary>
public class LineaTiempoService : ILineaTiempoService
{
    private readonly ILineaTiempoRepository _repository;
    private readonly ICitaRepository _citaRepository;
    private readonly ILogger<LineaTiempoService> _logger;

    public LineaTiempoService(
        ILineaTiempoRepository repository,
        ICitaRepository citaRepository,
        ILogger<LineaTiempoService> logger)
    {
        _repository = repository;
        _citaRepository = citaRepository;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene la línea de tiempo completa de una cita, ordenada por orden del paso.
    /// </summary>
    public async Task<ServiceResult<List<LineaTiempoResponseDto>>> GetTimelineByCitaAsync(Guid clinicaId, Guid citaId)
    {
        try
        {
            _logger.LogInformation("Obteniendo timeline de la cita {CitaId}", citaId);

            var entities = await _repository.GetByCitaIdAsync(clinicaId, citaId);
            var dtos = entities.Select(MapToDto).ToList();

            return ServiceResult<List<LineaTiempoResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener timeline de la cita {CitaId}", citaId);
            return ServiceResult<List<LineaTiempoResponseDto>>.Failure($"Error al obtener la línea de tiempo: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene la línea de tiempo del día para una clínica, opcionalmente filtrada por doctor.
    /// </summary>
    public async Task<ServiceResult<List<LineaTiempoResponseDto>>> GetTimelineDelDiaAsync(Guid clinicaId, Guid? doctorId, DateTime fecha)
    {
        try
        {
            _logger.LogInformation("Obteniendo timeline del día {Fecha} para clínica {ClinicaId}", fecha, clinicaId);

            var entities = await _repository.GetByClinicaAndDateAsync(clinicaId, doctorId, fecha);
            var dtos = entities.Select(MapToDto).ToList();

            return ServiceResult<List<LineaTiempoResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener timeline del día {Fecha}", fecha);
            return ServiceResult<List<LineaTiempoResponseDto>>.Failure($"Error al obtener la línea de tiempo del día: {ex.Message}");
        }
    }

    /// <summary>
    /// Inicia un paso de la línea de tiempo (cambia estado a "en_sala" y registra hora de llegada).
    /// </summary>
    public async Task<ServiceResult<LineaTiempoResponseDto>> IniciarPasoAsync(Guid clinicaId, Guid pasoId, Guid usuarioId)
    {
        try
        {
            _logger.LogInformation("Iniciando paso {PasoId}", pasoId);

            var paso = await _repository.GetByIdAsync(clinicaId, pasoId);
            if (paso == null)
            {
                return ServiceResult<LineaTiempoResponseDto>.Failure("Paso no encontrado.", ServiceErrorType.NotFound);
            }

            if (paso.Estado != "pendiente")
            {
                return ServiceResult<LineaTiempoResponseDto>.Failure(
                    $"El paso ya está en estado '{paso.Estado}'. Solo se pueden iniciar pasos pendientes.",
                    ServiceErrorType.Validation);
            }

            var horaActual = DateTime.UtcNow.TimeOfDay;
            var updated = await _repository.UpdateEstadoAsync(clinicaId, pasoId, "en_sala", horaActual);
            if (!updated)
            {
                return ServiceResult<LineaTiempoResponseDto>.Failure("No se pudo iniciar el paso.", ServiceErrorType.InternalError);
            }

            paso.Estado = "en_sala";
            paso.HoraLlegada = horaActual;
            paso.FechaModificacion = DateTime.UtcNow;

            return ServiceResult<LineaTiempoResponseDto>.Success(MapToDto(paso), "Paso iniciado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al iniciar paso {PasoId}", pasoId);
            return ServiceResult<LineaTiempoResponseDto>.Failure($"Error al iniciar el paso: {ex.Message}");
        }
    }

    /// <summary>
    /// Finaliza un paso de la línea de tiempo (cambia estado a "completado" y registra hora de salida).
    /// </summary>
    public async Task<ServiceResult<LineaTiempoResponseDto>> FinalizarPasoAsync(Guid clinicaId, Guid pasoId, Guid usuarioId)
    {
        try
        {
            _logger.LogInformation("Finalizando paso {PasoId}", pasoId);

            var paso = await _repository.GetByIdAsync(clinicaId, pasoId);
            if (paso == null)
            {
                return ServiceResult<LineaTiempoResponseDto>.Failure("Paso no encontrado.", ServiceErrorType.NotFound);
            }

            if (paso.Estado != "en_sala")
            {
                return ServiceResult<LineaTiempoResponseDto>.Failure(
                    $"El paso está en estado '{paso.Estado}'. Solo se pueden finalizar pasos en atención.",
                    ServiceErrorType.Validation);
            }

            var horaActual = DateTime.UtcNow.TimeOfDay;
            var updated = await _repository.UpdateEstadoAsync(clinicaId, pasoId, "completado", horaActual);
            if (!updated)
            {
                return ServiceResult<LineaTiempoResponseDto>.Failure("No se pudo finalizar el paso.", ServiceErrorType.InternalError);
            }

            paso.Estado = "completado";
            paso.HoraSalida = horaActual;
            paso.FechaModificacion = DateTime.UtcNow;

            // ── Verificar si todos los pasos están completados → marcar cita como atendida ──
            await VerificarYCompletarCitaAsync(clinicaId, paso.CitaId);

            return ServiceResult<LineaTiempoResponseDto>.Success(MapToDto(paso), "Paso finalizado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al finalizar paso {PasoId}", pasoId);
            return ServiceResult<LineaTiempoResponseDto>.Failure($"Error al finalizar el paso: {ex.Message}");
        }
    }

    /// <summary>
    /// Salta un paso de la línea de tiempo (cambia estado a "saltado" sin registrar tiempos).
    /// </summary>
    public async Task<ServiceResult<bool>> SaltarPasoAsync(Guid clinicaId, Guid pasoId)
    {
        try
        {
            _logger.LogInformation("Saltando paso {PasoId}", pasoId);

            var paso = await _repository.GetByIdAsync(clinicaId, pasoId);
            if (paso == null)
            {
                return ServiceResult<bool>.Failure("Paso no encontrado.", ServiceErrorType.NotFound);
            }

            if (paso.Estado != "pendiente")
            {
                return ServiceResult<bool>.Failure(
                    $"El paso está en estado '{paso.Estado}'. Solo se pueden saltar pasos pendientes.",
                    ServiceErrorType.Validation);
            }

            var updated = await _repository.UpdateEstadoAsync(clinicaId, pasoId, "saltado", null);
            if (!updated)
            {
                return ServiceResult<bool>.Failure("No se pudo saltar el paso.", ServiceErrorType.InternalError);
            }

            return ServiceResult<bool>.Success(true, "Paso saltado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al saltar paso {PasoId}", pasoId);
            return ServiceResult<bool>.Failure($"Error al saltar el paso: {ex.Message}");
        }
    }

    /// <summary>
    /// Genera los pasos predeterminados de línea de tiempo para una cita.
    /// Pasos por defecto: Llegada → Sala → Consulta → Diagnóstico → Salida.
    /// </summary>
    public async Task<ServiceResult<List<LineaTiempoResponseDto>>> GenerarPasosParaCitaAsync(Guid clinicaId, Guid citaId)
    {
        try
        {
            _logger.LogInformation("Generando pasos de timeline para la cita {CitaId}", citaId);

            // Verificar que la cita existe
            var cita = await _citaRepository.GetByIdAsync(clinicaId, citaId);
            if (cita == null)
            {
                return ServiceResult<List<LineaTiempoResponseDto>>.Failure("Cita no encontrada.", ServiceErrorType.NotFound);
            }

            // Verificar que no existan pasos ya generados
            var pasosExistentes = await _repository.GetByCitaIdAsync(clinicaId, citaId);
            if (pasosExistentes.Any())
            {
                return ServiceResult<List<LineaTiempoResponseDto>>.Failure("La cita ya tiene pasos de línea de tiempo generados.", ServiceErrorType.Conflict);
            }

            // Pasos por defecto
            var pasosDefault = new (string Nombre, int Orden)[]
            {
                ("Llegada", 1),
                ("Sala", 2),
                ("Consulta", 3),
                ("Diagnóstico", 4),
                ("Salida", 5)
            };

            var dtos = new List<LineaTiempoResponseDto>();

            foreach (var (nombre, orden) in pasosDefault)
            {
                var entity = new LineaTiempo
                {
                    ClinicaId = clinicaId,
                    CitaId = citaId,
                    PacienteId = cita.PacienteId,
                    SalaId = cita.SalaId,
                    NombrePaso = nombre,
                    Orden = orden,
                    Estado = "pendiente",
                    Activo = true,
                    FechaCreacion = DateTime.UtcNow
                };

                var id = await _repository.CreateAsync(entity);
                entity.Id = id;

                var dto = MapToDto(entity);
                dto.PacienteNombre = cita.PacienteNombre;
                dto.SalaNombre = cita.SalaNombre;
                dtos.Add(dto);
            }

            _logger.LogInformation("{Count} pasos generados para la cita {CitaId}", dtos.Count, citaId);
            return ServiceResult<List<LineaTiempoResponseDto>>.Success(dtos, "Pasos de línea de tiempo generados exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar pasos para la cita {CitaId}", citaId);
            return ServiceResult<List<LineaTiempoResponseDto>>.Failure($"Error al generar los pasos: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifica si todos los pasos de una cita están completados o saltados.
    /// Si es así, cambia el estado de la cita a "atendida".
    /// </summary>
    private async Task VerificarYCompletarCitaAsync(Guid clinicaId, Guid citaId)
    {
        try
        {
            var pasosRestantes = await _repository.GetByCitaIdAsync(clinicaId, citaId);
            if (pasosRestantes == null || !pasosRestantes.Any())
                return;

            // Todos los pasos deben estar en estado final (completado o saltado)
            var todosFinalizados = pasosRestantes.All(p =>
                p.Estado == "completado" || p.Estado == "saltado");

            if (!todosFinalizados)
            {
                _logger.LogInformation("Cita {CitaId} aún tiene pasos pendientes. No se marca como atendida.", citaId);
                return;
            }

            // Obtener la cita y actualizar su estado
            var cita = await _citaRepository.GetByIdAsync(clinicaId, citaId);
            if (cita == null)
            {
                _logger.LogWarning("Cita {CitaId} no encontrada al intentar marcarla como atendida.", citaId);
                return;
            }

            if (cita.Estado == "atendida")
            {
                _logger.LogInformation("Cita {CitaId} ya estaba marcada como atendida.", citaId);
                return;
            }

            cita.Estado = "atendida";
            cita.FechaModificacion = DateTime.UtcNow;
            await _citaRepository.UpdateAsync(cita);

            _logger.LogInformation("Cita {CitaId} marcada como atendida automáticamente (todos los pasos completados).", citaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al intentar marcar cita {CitaId} como atendida.", citaId);
            // No propagamos la excepción para no interrumpir el flujo principal
        }
    }

    // ── Mapeo Entity → DTO ──────────────────────────────────────────────

    private static LineaTiempoResponseDto MapToDto(LineaTiempo entity)
    {
        var dto = new LineaTiempoResponseDto
        {
            Id = entity.Id,
            ClinicaId = entity.ClinicaId,
            CitaId = entity.CitaId,
            PacienteId = entity.PacienteId,
            SalaId = entity.SalaId,
            NombrePaso = entity.NombrePaso,
            Orden = entity.Orden,
            Estado = entity.Estado,
            HoraLlegada = entity.HoraLlegada,
            HoraSalida = entity.HoraSalida,
            DuracionFormateada = FormatearDuracion(entity.HoraLlegada, entity.HoraSalida, entity.Estado),
            PacienteNombre = entity.PacienteNombre ?? string.Empty,
            SalaNombre = entity.SalaNombre
        };

        return dto;
    }

    /// <summary>
    /// Formatea la duración del paso.
    /// - "completado" o "saltado" con horaSalida → duración exacta entre llegada y salida
    /// - "en_sala" (activo) → duración desde la llegada hasta ahora (full current time)
    /// - "pendiente" o sin horaLlegada → "--:--:--"
    /// </summary>
    private static string FormatearDuracion(TimeSpan? horaLlegada, TimeSpan? horaSalida, string estado)
    {
        if (horaLlegada == null)
            return "--:--:--";

        // Si hay hora de salida (completado o saltado), calcular duración exacta
        if (horaSalida != null)
        {
            var duracion = horaSalida.Value - horaLlegada.Value;
            if (duracion < TimeSpan.Zero) duracion = TimeSpan.Zero;
            return duracion.ToString(@"hh\:mm\:ss");
        }

        // Si está activo (en_sala) o completado, calcular duración hasta ahora
        if (estado == "en_sala")
        {
            var ahora = DateTime.UtcNow.TimeOfDay;
            var duracion = ahora - horaLlegada.Value;
            if (duracion < TimeSpan.Zero) duracion = TimeSpan.Zero;
            return duracion.ToString(@"hh\:mm\:ss");
        }

        // Pendiente, saltado sin horaSalida, u otro → sin duración
        return "--:--:--";
    }

    /// <summary>
    /// Fuerza la verificación y completado de una cita. Revisa si todos los pasos están finalizados
    /// y, de ser así, marca la cita como "atendida". Útil para reparar citas atascadas.
    /// </summary>
    public async Task<ServiceResult<bool>> ForzarCompletarCitaAsync(Guid clinicaId, Guid citaId)
    {
        try
        {
            _logger.LogInformation("Forzando verificación de cita {CitaId}", citaId);
            await VerificarYCompletarCitaAsync(clinicaId, citaId);
            return ServiceResult<bool>.Success(true, "Verificación completada.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al forzar completado de cita {CitaId}", citaId);
            return ServiceResult<bool>.Failure($"Error al forzar completado: {ex.Message}");
        }
    }
}
