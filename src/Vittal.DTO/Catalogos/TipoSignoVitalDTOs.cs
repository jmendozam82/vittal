namespace Vittal.DTO.Catalogos;

public class TipoSignoVitalDTOs
{
    public class Request
    {
        public Guid SalaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Unidad { get; set; }
        public decimal? ValorMin { get; set; }
        public decimal? ValorMax { get; set; }
        public int Orden { get; set; }
        public bool EsObligatorio { get; set; }
    }

    public class Response
    {
        public Guid Id { get; set; }
        public Guid SalaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Unidad { get; set; }
        public decimal? ValorMin { get; set; }
        public decimal? ValorMax { get; set; }
        public int Orden { get; set; }
        public bool EsObligatorio { get; set; }
    }
}
