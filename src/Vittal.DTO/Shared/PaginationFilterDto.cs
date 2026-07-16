namespace Vittal.DTO.Shared;

/// <summary>
/// Filtro de paginación para consultas con paginación, ordenamiento y búsqueda.
/// </summary>
public class PaginationFilterDto
{
    /// <summary>Número de página (1-based).</summary>
    public int Page { get; set; } = 1;

    /// <summary>Tamaño de página (elementos por página).</summary>
    public int PageSize { get; set; } = 20;

    /// <summary>Columna por la que ordenar (ej: "primer_apellido").</summary>
    public string? SortBy { get; set; }

    /// <summary>Dirección del orden: "asc" o "desc".</summary>
    public string? SortDirection { get; set; } = "asc";

    /// <summary>Término de búsqueda opcional (ILike).</summary>
    public string? SearchTerm { get; set; }
}
