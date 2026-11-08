namespace NotesApi.Models;

/// <summary>Vista resumida para listados: no arrastra el contenido completo de la nota.</summary>
public sealed record NoteSummary
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
