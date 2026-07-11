using System.ComponentModel.DataAnnotations;

namespace NotesApi.Models;

/// <summary>Cuerpo de las peticiones de creación y actualización de notas.</summary>
public class NoteRequest
{
    [Required(ErrorMessage = "El título es obligatorio.")]
    [MaxLength(250, ErrorMessage = "El título no puede superar los 250 caracteres.")]
    public string Title { get; set; } = string.Empty;

    [MaxLength(100_000, ErrorMessage = "El contenido no puede superar los 100.000 caracteres.")]
    public string? Text { get; set; }
}
