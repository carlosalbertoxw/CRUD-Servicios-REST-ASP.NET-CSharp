namespace NotesApi.Models;

/// <summary>
/// Página de resultados con paginación por keyset: para obtener la siguiente
/// página se envía ?afterId={NextAfterId}. Costo constante en la BD sin
/// importar qué tan profunda sea la página (a diferencia de LIMIT/OFFSET).
/// No incluye un total: contarlo exigiría recorrer todas las notas del cliente
/// en cada página; NextAfterId ya indica si hay más resultados.
/// </summary>
public sealed record PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int PageSize { get; init; }

    /// <summary>Cursor para la siguiente página, o null si no hay más resultados.</summary>
    public int? NextAfterId { get; init; }
}
