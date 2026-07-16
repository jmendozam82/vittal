namespace Vittal.DTO.Shared;

/// <summary>
/// Respuesta paginada genérica para cualquier tipo de dato T.
/// </summary>
public class PaginatedResultDto<T>
{
    /// <summary>Elementos de la página actual.</summary>
    public IEnumerable<T> Items { get; set; } = new List<T>();

    /// <summary>Total de registros (sin paginación).</summary>
    public int TotalCount { get; set; }

    /// <summary>Número de página actual (1-based).</summary>
    public int Page { get; set; }

    /// <summary>Tamaño de página.</summary>
    public int PageSize { get; set; }

    /// <summary>Total de páginas calculado.</summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
