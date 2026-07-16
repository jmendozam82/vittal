namespace Vittal.Entity;

/// <summary>
/// Alerta generada cuando un paciente excede el tiempo de espera configurado.
/// Tabla: public.alertas_espera
/// Historia de Usuario: HU23 — Alertas Configurables
/// </summary>
public class AlertaEspera
{
    // ── Campos primarios ──────────────────────────────────────────
    public Guid Id { get; set; }
    public Guid ClinicaId { get; set; }
    public Guid CitaId { get; set; }
    public Guid PacienteId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid? SalaId { get; set; }

    // ── Campos de tiempo ──────────────────────────────────────────
    /// <summary>Hora programada de la cita (TIME en BD).</summary>
    public TimeOnly HoraCita { get; set; }

    /// <summary>Hora en que el paciente llegó (nullable).</summary>
    public TimeOnly? HoraLlegada { get; set; }

    /// <summary>Minutos de espera al momento de generar la alerta.</summary>
    public int MinutosEspera { get; set; }

    // ── Campos de estado ──────────────────────────────────────────
    /// <summary>TRUE si la alerta fue atendida/resuelta.</summary>
    public bool Resuelta { get; set; }

    /// <summary>Fecha y hora en que se generó la alerta.</summary>
    public DateTime FechaAlerta { get; set; } = DateTime.UtcNow;

    /// <summary>Fecha y hora en que se resolvió la alerta.</summary>
    public DateTime? FechaResolucion { get; set; }

    // ── Propiedades calculadas / JOIN (no se persisten directamente) ──
    /// <summary>Nombre completo del paciente (JOIN con pacientes).</summary>
    public string PacienteNombre { get; set; } = string.Empty;

    /// <summary>Nombre completo del doctor (JOIN con usuarios).</summary>
    public string DoctorNombre { get; set; } = string.Empty;

    /// <summary>Nombre de la sala (JOIN con salas).</summary>
    public string? SalaNombre { get; set; }

    /// <summary>
    /// Convierte AlertaEspera → Notificacion para cumplir con la interfaz IAlertaEsperaRepository.
    /// </summary>
    public Notificacion ToNotificacion()
    {
        return new Notificacion
        {
            Id = this.Id,
            ClinicaId = this.ClinicaId,
            AlertaId = this.Id,
            Tipo = "alerta_espera",
            Titulo = $"Paciente en espera: {this.PacienteNombre}",
            Mensaje = $"{this.PacienteNombre} lleva {this.MinutosEspera} min esperando. Doctor: {this.DoctorNombre}.",
            Icono = "clock",
            Color = "warning",
            Leida = false,
            UsuarioDestinoId = null,
            FechaLectura = null,
            Activo = true,
            FechaCreacion = this.FechaAlerta
        };
    }
}
