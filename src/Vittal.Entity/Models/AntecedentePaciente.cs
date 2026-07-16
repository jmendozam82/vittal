namespace Vittal.Entity;

/// <summary>
/// Antecedentes médicos registrados por paciente, organizados por sala.
/// Tabla: public.antecedentes_paciente
/// Historia de Usuario: HU-E05 — Antecedentes del Paciente
/// </summary>
public class AntecedentePaciente
{
    public Guid Id { get; set; }

    /// <summary>
    /// Discriminador de tenant (solo para RLS)
    /// </summary>
    public Guid ClinicaId { get; set; }

    /// <summary>
    /// Expediente médico del paciente al que pertenece este antecedente
    /// </summary>
    public Guid ExpedienteId { get; set; }

    /// <summary>
    /// Sala (especialidad) donde se registró el antecedente
    /// </summary>
    public Guid SalaId { get; set; }

    /// <summary>
    /// Tipo de antecedente (referencia al catálogo tipos_antecedente)
    /// </summary>
    public Guid TipoAntecedenteId { get; set; }

    /// <summary>
    /// Valor del antecedente según el TipoDato del tipo:
    /// 'boolean' → "true"/"false", 'texto' → texto libre, 'numero' → valor numérico como string
    /// </summary>
    public string Valor { get; set; } = string.Empty;

    /// <summary>
    /// Fecha de la última actualización del valor del antecedente
    /// </summary>
    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Usuario que realizó la última actualización
    /// </summary>
    public Guid? ActualizadoPor { get; set; }

    // Auditoría base
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaModificacion { get; set; }

    // Relaciones
    public Clinica? Clinica { get; set; }
    public Sala? Sala { get; set; }
    public TipoAntecedente? TipoAntecedente { get; set; }
    public Usuario? Actualizador { get; set; }
}
