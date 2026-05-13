namespace Vittal.DTO.AntecedentesPaciente;

/// <summary>
/// DTOs para el módulo de Antecedentes del Paciente (HU-E05)
/// </summary>
public class AntecedentePacienteDTOs
{
    public class Request
    {
        public Guid ExpedienteId { get; set; }
        public Guid SalaId { get; set; }
        public Guid TipoAntecedenteId { get; set; }
        public string Valor { get; set; } = string.Empty;
    }

    public class Response
    {
        public Guid Id { get; set; }
        public Guid ExpedienteId { get; set; }
        public Guid SalaId { get; set; }
        public string SalaNombre { get; set; } = string.Empty;
        public Guid TipoAntecedenteId { get; set; }
        public string TipoAntecedenteNombre { get; set; } = string.Empty;
        public string? TipoAntecedenteCategoria { get; set; }
        public string TipoAntecedenteTipoDato { get; set; } = string.Empty;
        public string Valor { get; set; } = string.Empty;
        public DateTime FechaActualizacion { get; set; }
        public Guid? ActualizadoPor { get; set; }
    }
}
