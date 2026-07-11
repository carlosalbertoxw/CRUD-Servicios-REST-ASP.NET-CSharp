namespace NotesApi.Models;

/// <summary>
/// Página de resultados con paginación por keyset: para obtener la siguiente
/// página se envía ?afterId={NextAfterId}. Costo constante en la BD sin
/// importar qué tan profunda sea la página (a diferencia de LIMIT/OFFSET).
/// </summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int PageSize { get; init; }
    public long TotalCount { get; init; }

    /// <summary>Cursor para la siguiente página, o null si no hay más resultados.</summary>
    public int? NextAfterId { get; init; }
}
