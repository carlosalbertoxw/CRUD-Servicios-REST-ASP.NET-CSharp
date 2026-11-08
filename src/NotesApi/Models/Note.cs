namespace NotesApi.Models;

/// <summary>Nota con su contenido completo. Las fechas son UTC (DateTimeKind.Utc).</summary>
public sealed record Note
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Text { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
