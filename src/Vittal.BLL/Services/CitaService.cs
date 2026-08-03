using Microsoft.Extensions.Logging;
using Vittal.BLL.Interfaces;
using Vittal.DAL.Interfaces;
using Vittal.DTO.Cita;
using Vittal.Entity;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de lógica de negocio para citas médicas.
/// Historia de Usuario: HU21 — Agenda (HU-E01 — hora_fin)
/// </summary>
public class CitaService : ICitaService
{
    private readonly ICitaRepository _repository;
    private readonly ILineaTiempoService _lineaTiempoService;
    private readonly IClinicaService _clinicaService;
    private readonly ILogger<CitaService> _logger;

    public CitaService(
        ICitaRepository repository,
        ILineaTiempoService lineaTiempoService,
        IClinicaService clinicaService,
        ILogger<CitaService> logger)
    {
        _repository = repository;
        _lineaTiempoService = lineaTiempoService;
        _clinicaService = clinicaService;
        _logger = logger;
    }

    // ── Mapeo de días de atención ─────────────────────────────────
    // DiasAtencion en BD: "L,M,MI,J,V" (abreviaturas en español)
    // JS DayOfWeek: 0=Dom, 1=Lun, 2=Mar, 3=Mié, 4=Jue, 5=Vie, 6=Sáb
    private static readonly Dictionary<int, string> DayMapping = new()
    {
        { 1, "L" },    // Lunes
        { 2, "M" },    // Martes
        { 3, "MI" },   // Miércoles
        { 4, "J" },    // Jueves
        { 5, "V" },    // Viernes
        { 6, "S" },    // Sábado
        { 0, "D" }     // Domingo
    };

    /// <summary>
    /// Valida si una fecha y hora están dentro del horario de atención de la clínica.
    /// Retorna null si es válido, o un mensaje de error si no lo es.
    /// </summary>
    private async Task<string?> ValidarHorarioAtencionAsync(
        DateOnly fechaCita, TimeOnly horaCita, TimeOnly? horaFin, Guid clinicaId)
    {
        var clinicaResult = await _clinicaService.GetByIdAsync(clinicaId);
        if (!clinicaResult.IsSuccess || clinicaResult.Data == null)
        {
            _logger.LogWarning("No se pudo cargar la clínica {ClinicaId} para validar horario", clinicaId);
            return null; // Si no se puede cargar la clínica, no bloquear
        }

        var clinica = clinicaResult.Data;

        // Si no tiene horarios configurados, no validar
        if (string.IsNullOrWhiteSpace(clinica.HorarioApertura) ||
            string.IsNullOrWhiteSpace(clinica.HorarioCierre) ||
            string.IsNullOrWhiteSpace(clinica.DiasAtencion))
        {
            return null;
        }

        // 1. Validar día de atención
        var dayOfWeek = fechaCita.DayOfWeek;
        if (!DayMapping.TryGetValue((int)dayOfWeek, out var dayCode))
        {
            return null;
        }

        var diasAtencion = clinica.DiasAtencion
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (!diasAtencion.Contains(dayCode))
        {
            var diaNombre = dayOfWeek switch
            {
                DayOfWeek.Monday => "Lunes",
                DayOfWeek.Tuesday => "Martes",
                DayOfWeek.Wednesday => "Miércoles",
                DayOfWeek.Thursday => "Jueves",
                DayOfWeek.Friday => "Viernes",
                DayOfWeek.Saturday => "Sábado",
                DayOfWeek.Sunday => "Domingo",
                _ => dayOfWeek.ToString()
            };
            return $"La clínica no atiende los días {diaNombre}. Días de atención: {string.Join(", ", diasAtencion)}";
        }

        // 2. Validar rango de horas
        if (TimeOnly.TryParse(clinica.HorarioApertura, out var apertura) &&
            TimeOnly.TryParse(clinica.HorarioCierre, out var cierre))
        {
            if (horaCita < apertura || horaCita >= cierre)
            {
                return $"La hora de inicio ({horaCita:HH:mm}) está fuera del horario de atención ({apertura:HH:mm} — {cierre:HH:mm}).";
            }

            // Validar hora_fin si se proporciona
            if (horaFin.HasValue && horaFin.Value > cierre)
            {
                return $"La hora de fin ({horaFin.Value:HH:mm}) excede el horario de cierre ({cierre:HH:mm}).";
            }

            // Validar que hora_fin > hora_cita
            if (horaFin.HasValue && horaFin.Value <= horaCita)
            {
                return "La hora de fin debe ser posterior a la hora de inicio.";
            }
        }

        return null;
    }

    /// <summary>
    /// Valida que una cita nueva no esté programada en el pasado (fecha o hora de hoy).
    /// Se usa SOLO al crear; la edición permite fechas retroactivas.
    /// Comparación con precisión de segundos.
    /// </summary>
    private static string? ValidarCitaEnPasado(DateOnly fechaCita, TimeOnly horaCita)
    {
        var ahora = DateTime.Now;
        var hoy = DateOnly.FromDateTime(ahora);

        if (fechaCita < hoy)
        {
            return "No se pueden agendar citas en fechas pasadas.";
        }

        if (fechaCita == hoy && horaCita < TimeOnly.FromDateTime(ahora))
        {
            return $"La hora de inicio ({horaCita:HH:mm:ss}) ya pasó. Seleccione una hora posterior a las {ahora:HH:mm:ss}.";
        }

        return null;
    }

    public async Task<ServiceResult<IEnumerable<CitaResponseDto>>> GetAllAsync(Guid clinicaId)
    {
        try
        {
            var entities = await _repository.GetAllAsync(clinicaId);
            var dtos = entities.Select(MapToDto);
            return ServiceResult<IEnumerable<CitaResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en GetAllAsync de CitaService");
            return ServiceResult<IEnumerable<CitaResponseDto>>.Failure("Error al obtener las citas.");
        }
    }

    public async Task<ServiceResult<CitaResponseDto>> GetByIdAsync(Guid clinicaId, Guid id)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(clinicaId, id);
            if (entity == null)
            {
                return ServiceResult<CitaResponseDto>.Failure("Cita no encontrada.", ServiceErrorType.NotFound);
            }

            var dto = MapToDto(entity);
            return ServiceResult<CitaResponseDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en GetByIdAsync de CitaService");
            return ServiceResult<CitaResponseDto>.Failure("Error al obtener la cita.");
        }
    }

    public async Task<ServiceResult<CitaResponseDto>> CreateAsync(CitaRequestDto dto, Guid clinicaId, Guid creadoPor)
    {
        try
        {
            // ── Validar horario de atención de la clínica ──────────────
            var horarioError = await ValidarHorarioAtencionAsync(
                dto.FechaCita, dto.HoraCita, dto.HoraFin, clinicaId);
            if (horarioError != null)
            {
                _logger.LogWarning("Validación de horario rechazó cita: {Error}", horarioError);
                return ServiceResult<CitaResponseDto>.Failure(horarioError, ServiceErrorType.Validation);
            }

            // ── Validar que la cita no esté en el pasado (solo al crear) ──
            var pasadoError = ValidarCitaEnPasado(dto.FechaCita, dto.HoraCita);
            if (pasadoError != null)
            {
                _logger.LogWarning("Validación de fecha/hora rechazó cita: {Error}", pasadoError);
                return ServiceResult<CitaResponseDto>.Failure(pasadoError, ServiceErrorType.Validation);
            }

            var entity = new Cita
            {
                ClinicaId = clinicaId,
                PacienteId = dto.PacienteId,
                DoctorId = dto.DoctorId,
                SalaId = dto.SalaId,
                FechaCita = dto.FechaCita,
                HoraCita = dto.HoraCita,
                HoraFin = dto.HoraFin,
                HoraLlegada = dto.HoraLlegada,
                Lugar = dto.Lugar,
                Motivo = dto.Motivo,
                Estado = dto.Estado,
                Notas = dto.Notas,
                Activo = true,
                FechaCreacion = DateTime.UtcNow,
                CreadoPor = creadoPor
            };

            var id = await _repository.CreateAsync(entity);

            // Recuperar la entidad creada para obtener los nombres de JOIN
            var created = await _repository.GetByIdAsync(clinicaId, id);
            if (created == null)
            {
                return ServiceResult<CitaResponseDto>.Failure("Error al recuperar la cita creada.", ServiceErrorType.InternalError);
            }

            // Generar pasos de línea de tiempo automáticamente (HU19)
            await GenerarLineaTiempoAsync(clinicaId, id);

            var responseDto = MapToDto(created);
            return ServiceResult<CitaResponseDto>.Success(responseDto, "Cita creada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en CreateAsync de CitaService");
            return ServiceResult<CitaResponseDto>.Failure("Error al crear la cita.");
        }
    }

    public async Task<ServiceResult<CitaResponseDto>> UpdateAsync(Guid id, CitaRequestDto dto, Guid clinicaId)
    {
        try
        {
            var entity = await _repository.GetByIdAsync(clinicaId, id);
            if (entity == null)
            {
                return ServiceResult<CitaResponseDto>.Failure("Cita no encontrada.", ServiceErrorType.NotFound);
            }

            // ── Validar horario de atención de la clínica ──────────────
            var horarioError = await ValidarHorarioAtencionAsync(
                dto.FechaCita, dto.HoraCita, dto.HoraFin, clinicaId);
            if (horarioError != null)
            {
                _logger.LogWarning("Validación de horario rechazó actualización de cita: {Error}", horarioError);
                return ServiceResult<CitaResponseDto>.Failure(horarioError, ServiceErrorType.Validation);
            }

            entity.PacienteId = dto.PacienteId;
            entity.DoctorId = dto.DoctorId;
            entity.SalaId = dto.SalaId;
            entity.FechaCita = dto.FechaCita;
            entity.HoraCita = dto.HoraCita;
            entity.HoraFin = dto.HoraFin;
            entity.HoraLlegada = dto.HoraLlegada;
            entity.Lugar = dto.Lugar;
            entity.Motivo = dto.Motivo;
            entity.Estado = dto.Estado;
            entity.Notas = dto.Notas;
            entity.FechaModificacion = DateTime.UtcNow;

            var result = await _repository.UpdateAsync(entity);
            if (!result)
            {
                return ServiceResult<CitaResponseDto>.Failure("No se pudo actualizar la cita.");
            }

            var updated = await _repository.GetByIdAsync(clinicaId, id);
            var responseDto = MapToDto(updated ?? entity);
            return ServiceResult<CitaResponseDto>.Success(responseDto, "Cita actualizada exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en UpdateAsync de CitaService");
            return ServiceResult<CitaResponseDto>.Failure("Error al actualizar la cita.");
        }
    }

    public async Task<ServiceResult<bool>> DeactivateAsync(Guid clinicaId, Guid id)
    {
        try
        {
            var result = await _repository.DeactivateAsync(clinicaId, id);
            return result
                ? ServiceResult<bool>.Success(result, "Cita desactivada exitosamente.")
                : ServiceResult<bool>.Failure("No se encontró la cita.", ServiceErrorType.NotFound);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en DeactivateAsync de CitaService");
            return ServiceResult<bool>.Failure("Error al desactivar la cita.");
        }
    }

    // ── Mapeo manual Entity → DTO ──────────────────────────────────────

    /// <summary>
    /// Genera los pasos de línea de tiempo para una cita de forma no-bloqueante.
    /// Si falla, solo se loguea la advertencia — no debe impedir la creación de la cita.
    /// </summary>
    private async Task GenerarLineaTiempoAsync(Guid clinicaId, Guid citaId)
    {
        try
        {
            var result = await _lineaTiempoService.GenerarPasosParaCitaAsync(clinicaId, citaId);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("No se generó la línea de tiempo para cita {CitaId}: {Error}", citaId, result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error no crítico al generar línea de tiempo para cita {CitaId}", citaId);
        }
    }

    private static CitaResponseDto MapToDto(Cita entity)
    {
        return new CitaResponseDto
        {
            Id = entity.Id,
            ClinicaId = entity.ClinicaId,
            PacienteId = entity.PacienteId,
            DoctorId = entity.DoctorId,
            SalaId = entity.SalaId,
            FechaCita = entity.FechaCita,
            HoraCita = entity.HoraCita,
            HoraFin = entity.HoraFin,
            HoraLlegada = entity.HoraLlegada,
            Lugar = entity.Lugar,
            Motivo = entity.Motivo,
            Estado = entity.Estado,
            Notas = entity.Notas,
            Activo = entity.Activo,
            FechaCreacion = entity.FechaCreacion,
            FechaModificacion = entity.FechaModificacion,
            PacienteNombre = entity.PacienteNombre,
            DoctorNombre = entity.DoctorNombre,
            SalaNombre = entity.SalaNombre
        };
    }
}
