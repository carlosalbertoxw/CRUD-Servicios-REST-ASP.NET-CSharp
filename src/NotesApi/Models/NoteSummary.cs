namespace NotesApi.Models;

/// <summary>Vista resumida para listados: no arrastra el contenido completo de la nota.</summary>
public class NoteSummary
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
