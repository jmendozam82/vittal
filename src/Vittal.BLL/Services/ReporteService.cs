using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Vittal.BLL.Interfaces;
using Vittal.DAL.Interfaces;
using Vittal.DTO.Reporte;
using Vittal.Entity;
using Vittal.Utility.Results;

namespace Vittal.BLL.Services;

/// <summary>
/// Servicio de reportes del sistema.
/// Historia de Usuario: HU22 — Reportes
/// </summary>
public class ReporteService : IReporteService
{
    private readonly IReporteRepository _reporteRepository;
    private readonly ICitaRepository _citaRepository;
    private readonly ILogger<ReporteService> _logger;

    public ReporteService(
        IReporteRepository reporteRepository,
        ICitaRepository citaRepository,
        ILogger<ReporteService> logger)
    {
        _reporteRepository = reporteRepository;
        _citaRepository = citaRepository;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todos los reportes generados de la clínica.
    /// </summary>
    public async Task<ServiceResult<List<ReporteResponseDto>>> GetAllAsync(Guid clinicaId)
    {
        try
        {
            _logger.LogInformation("Obteniendo reportes para clínica {ClinicaId}", clinicaId);

            var entities = await _reporteRepository.GetAllByClinicaIdAsync(clinicaId);
            var dtos = entities.Select(MapToDto).ToList();

            return ServiceResult<List<ReporteResponseDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener reportes para clínica {ClinicaId}", clinicaId);
            return ServiceResult<List<ReporteResponseDto>>.Failure($"Error al obtener reportes: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene un reporte por ID.
    /// </summary>
    public async Task<ServiceResult<ReporteResponseDto>> GetByIdAsync(Guid clinicaId, Guid id)
    {
        try
        {
            _logger.LogInformation("Obteniendo reporte {Id}", id);

            var entity = await _reporteRepository.GetByIdAsync(clinicaId, id);
            if (entity == null)
            {
                return ServiceResult<ReporteResponseDto>.Failure("Reporte no encontrado.", ServiceErrorType.NotFound);
            }

            return ServiceResult<ReporteResponseDto>.Success(MapToDto(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener reporte {Id}", id);
            return ServiceResult<ReporteResponseDto>.Failure($"Error al obtener el reporte: {ex.Message}");
        }
    }

    /// <summary>
    /// Genera un nuevo reporte con los filtros especificados.
    /// Tipos soportados: pacientes_por_dia, citas_por_estado, doctores_mas_activos, tiempo_promedio_espera.
    /// </summary>
    public async Task<ServiceResult<ReporteResponseDto>> GenerarReporteAsync(ReporteRequestDto dto, Guid clinicaId, Guid usuarioId)
    {
        try
        {
            _logger.LogInformation("Generando reporte tipo {Tipo} para clínica {ClinicaId}", dto.Tipo, clinicaId);

            // Validar tipo de reporte
            var tiposValidos = new[] { "pacientes_por_dia", "citas_por_estado", "doctores_mas_activos", "tiempo_promedio_espera", "tiempos_espera", "citas_atendidas", "pacientes_atendidos", "ingresos", "historial_citas", "cirugias", "examenes" };
            if (!tiposValidos.Contains(dto.Tipo))
            {
                return ServiceResult<ReporteResponseDto>.Failure(
                    $"Tipo de reporte '{dto.Tipo}' no válido. Tipos: {string.Join(", ", tiposValidos)}",
                    ServiceErrorType.Validation);
            }

            // Validar rango de fechas
            if (dto.FechaInicio > dto.FechaFin)
            {
                return ServiceResult<ReporteResponseDto>.Failure(
                    "La fecha de inicio no puede ser mayor a la fecha de fin.",
                    ServiceErrorType.Validation);
            }

            // Ejecutar la consulta de agregación según el tipo
            var contenidoJson = await _reporteRepository.ExecuteReportQueryAsync(
                dto.Tipo, clinicaId, dto.FechaInicio, dto.FechaFin, dto.DoctorId, dto.SalaId);

            // Construir nombre del reporte
            var nombreReporte = dto.Tipo switch
            {
                "pacientes_por_dia" => "Pacientes por día",
                "citas_por_estado" => "Citas por estado",
                "doctores_mas_activos" => "Doctores más activos",
                "tiempo_promedio_espera" => "Tiempo promedio de espera",
                "tiempos_espera" => "Tiempos de espera",
                "citas_atendidas" => "Citas atendidas",
                "pacientes_atendidos" => "Pacientes atendidos",
                "ingresos" => "Ingresos",
                "historial_citas" => "Historial de Citas",
                "cirugias" => "Cirugías",
                "examenes" => "Exámenes",
                _ => dto.Tipo
            };

            // Guardar reporte
            var reporte = new Reporte
            {
                ClinicaId = clinicaId,
                Nombre = $"{nombreReporte} - {dto.FechaInicio:dd/MM/yyyy} al {dto.FechaFin:dd/MM/yyyy}",
                Tipo = dto.Tipo,
                Descripcion = $"Reporte {nombreReporte} del {dto.FechaInicio:dd/MM/yyyy} al {dto.FechaFin:dd/MM/yyyy}",
                Formato = dto.Formato,
                ContenidoJson = contenidoJson,
                FechaInicio = dto.FechaInicio,
                FechaFin = dto.FechaFin,
                Activo = true,
                FechaCreacion = DateTime.UtcNow,
                CreadoPor = usuarioId
            };

            var id = await _reporteRepository.CreateAsync(reporte);
            reporte.Id = id;

            _logger.LogInformation("Reporte {Id} generado exitosamente, tipo: {Tipo}", id, dto.Tipo);
            return ServiceResult<ReporteResponseDto>.Success(MapToDto(reporte), "Reporte generado exitosamente.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al generar reporte tipo {Tipo}", dto.Tipo);
            return ServiceResult<ReporteResponseDto>.Failure($"Error al generar el reporte: {ex.Message}");
        }
    }

    /// <summary>
    /// Exporta un reporte existente en el formato especificado.
    /// </summary>
    public async Task<ServiceResult<byte[]>> ExportarAsync(Guid clinicaId, Guid id, string formato)
    {
        try
        {
            _logger.LogInformation("Exportando reporte {Id} en formato {Formato}", id, formato);

            var entity = await _reporteRepository.GetByIdAsync(clinicaId, id);
            if (entity == null)
            {
                return ServiceResult<byte[]>.Failure("Reporte no encontrado.", ServiceErrorType.NotFound);
            }

            // Validar formato
            var formatosValidos = new[] { "pdf", "excel", "csv", "json" };
            if (!formatosValidos.Contains(formato))
            {
                return ServiceResult<byte[]>.Failure(
                    $"Formato '{formato}' no válido. Formatos: {string.Join(", ", formatosValidos)}",
                    ServiceErrorType.Validation);
            }

            byte[] data;

            switch (formato)
            {
                case "csv":
                    data = ConvertJsonToCsv(entity.ContenidoJson);
                    break;
                case "json":
                    data = Encoding.UTF8.GetBytes(entity.ContenidoJson);
                    break;
                case "pdf":
                case "excel":
                default:
                    // Para PDF y Excel, retornamos el JSON como texto plano
                    // Se implementará la generación real cuando se agregue la librería correspondiente
                    data = Encoding.UTF8.GetBytes(entity.ContenidoJson);
                    break;
            }

            return ServiceResult<byte[]>.Success(data, $"Reporte exportado como {formato}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al exportar reporte {Id}", id);
            return ServiceResult<byte[]>.Failure($"Error al exportar el reporte: {ex.Message}");
        }
    }

    // ── Mapeo Entity → DTO ──────────────────────────────────────────────

    private static ReporteResponseDto MapToDto(Reporte entity)
    {
        return new ReporteResponseDto
        {
            Id = entity.Id,
            Nombre = entity.Nombre,
            Tipo = entity.Tipo,
            FechaCreacion = entity.FechaCreacion,
            ContenidoJson = entity.ContenidoJson,
            Formato = entity.Formato
        };
    }

    // ── Métodos auxiliares ──────────────────────────────────────────────

    /// <summary>
    /// Convierte JSON a CSV de forma básica. Asume un array de objetos planos.
    /// </summary>
    private static byte[] ConvertJsonToCsv(string json)
    {
        try
        {
            var sb = new StringBuilder();
            var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind == System.Text.Json.JsonValueKind.Array && root.GetArrayLength() > 0)
            {
                // Extraer headers del primer elemento
                var first = root[0];
                var headers = new List<string>();
                foreach (var prop in first.EnumerateObject())
                {
                    headers.Add(prop.Name);
                }
                sb.AppendLine(string.Join(",", headers));

                // Extraer datos de cada elemento
                foreach (var item in root.EnumerateArray())
                {
                    var values = new List<string>();
                    foreach (var header in headers)
                    {
                        if (item.TryGetProperty(header, out var prop))
                        {
                            values.Add(prop.ValueKind == System.Text.Json.JsonValueKind.String
                                ? $"\"{prop.GetString()}\""
                                : prop.GetRawText());
                        }
                        else
                        {
                            values.Add("");
                        }
                    }
                    sb.AppendLine(string.Join(",", values));
                }
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }
        catch
        {
            return Encoding.UTF8.GetBytes(json);
        }
    }
}
