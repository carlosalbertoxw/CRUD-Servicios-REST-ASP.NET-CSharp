using System.ComponentModel.DataAnnotations;

namespace NotesApi.Data;

/// <summary>
/// Cadenas de conexión validadas al arranque (ValidateOnStart): si falta la
/// configuración, la instancia muere al iniciar en vez de fallar con tráfico.
/// </summary>
public class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";

    [Required(ErrorMessage = "Falta la cadena de conexión 'ConnectionStrings:NotesDb'.")]
    public string NotesDb { get; set; } = string.Empty;
}
