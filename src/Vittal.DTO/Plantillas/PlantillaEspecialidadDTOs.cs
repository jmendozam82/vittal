namespace Vittal.DTO.Plantillas;

public class PlantillaEspecialidadDTOs
{
    public class Request
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? Icono { get; set; }
    }

    public class Response
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? Icono { get; set; }
        public IEnumerable<PlantillaItemDTOs.Response> Items { get; set; } = new List<PlantillaItemDTOs.Response>();
    }
}

public class PlantillaItemDTOs
{
    public class Request
    {
        public Guid PlantillaId { get; set; }
        public string TipoItem { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Categoria { get; set; }
        public string TipoDato { get; set; } = "boolean";
        public string? Unidad { get; set; }
        public decimal? ValorMin { get; set; }
        public decimal? ValorMax { get; set; }
        public bool EsObligatorio { get; set; }
        public int Orden { get; set; }
    }

    public class Response
    {
        public Guid Id { get; set; }
        public Guid PlantillaId { get; set; }
        public string TipoItem { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Categoria { get; set; }
        public string TipoDato { get; set; } = "boolean";
        public string? Unidad { get; set; }
        public decimal? ValorMin { get; set; }
        public decimal? ValorMax { get; set; }
        public bool EsObligatorio { get; set; }
        public int Orden { get; set; }
    }
}
