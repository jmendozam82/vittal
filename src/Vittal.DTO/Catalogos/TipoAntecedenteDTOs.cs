namespace Vittal.DTO.Catalogos;

public class TipoAntecedenteDTOs
{
    public class Request
    {
        public Guid SalaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Categoria { get; set; }
        public string TipoDato { get; set; } = "boolean";
        public int Orden { get; set; }
    }

    public class Response
    {
        public Guid Id { get; set; }
        public Guid SalaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Categoria { get; set; }
        public string TipoDato { get; set; } = "boolean";
        public int Orden { get; set; }
    }
}
