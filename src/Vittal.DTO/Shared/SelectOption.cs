namespace Vittal.DTO.Shared;

/// <summary>
/// Modelo auxiliar para opciones de selección en filtros y combos.
/// </summary>
public class SelectOption
{
    /// <summary>Valor interno del elemento (GUID, ID, etc.).</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Etiqueta visible para el usuario.</summary>
    public string Label { get; set; } = string.Empty;
}
