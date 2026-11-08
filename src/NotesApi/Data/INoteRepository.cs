using NotesApi.Models;

namespace NotesApi.Data;

/// <summary>
/// Todas las operaciones están acotadas al dueño (ownerKeyId, el key_id
/// autenticado): un cliente nunca ve ni modifica notas de otro.
/// </summary>
public interface INoteRepository
{
    Task<PagedResult<NoteSummary>> GetPageAsync(
        string ownerKeyId, int afterId, int pageSize, string? search, CancellationToken cancellationToken);

    Task<Note?> GetByIdAsync(string ownerKeyId, int id, CancellationToken cancellationToken);
    Task<Note> CreateAsync(string ownerKeyId, NoteRequest request, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(string ownerKeyId, int id, NoteRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string ownerKeyId, int id, CancellationToken cancellationToken);
}
