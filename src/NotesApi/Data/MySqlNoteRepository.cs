using Dapper;
using MySqlConnector;
using NotesApi.Models;

namespace NotesApi.Data;

public class MySqlNoteRepository : INoteRepository
{
    private readonly MySqlDataSource _dataSource;

    public MySqlNoteRepository(MySqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<PagedResult<NoteSummary>> GetPageAsync(
        string ownerKeyId, int afterId, int pageSize, string? search, CancellationToken cancellationToken)
    {
        var filter = "owner_key_id = @ownerKeyId";
        if (!string.IsNullOrWhiteSpace(search))
        {
            filter += " AND MATCH(title, text) AGAINST(@search IN NATURAL LANGUAGE MODE)";
        }

        await using var connection = _dataSource.CreateConnection();

        // Keyset: se pide un elemento extra para saber si hay página siguiente.
        var items = (await connection.QueryAsync<NoteSummary>(new CommandDefinition(
            $"""
            SELECT id, title, created_at, updated_at
            FROM notes
            WHERE {filter} AND id > @afterId
            ORDER BY id
            LIMIT @limit;
            """,
            new { ownerKeyId, search, afterId, limit = pageSize + 1 },
            cancellationToken: cancellationToken))).ToList();

        var hasMore = items.Count > pageSize;
        if (hasMore)
        {
            items.RemoveAt(items.Count - 1);
        }

        return new PagedResult<NoteSummary>
        {
            Items = items,
            PageSize = pageSize,
            NextAfterId = hasMore ? items[^1].Id : null,
        };
    }

    public async Task<Note?> GetByIdAsync(string ownerKeyId, int id, CancellationToken cancellationToken)
    {
        await using var connection = _dataSource.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Note>(new CommandDefinition(
            """
            SELECT id, title, text, created_at, updated_at
            FROM notes
            WHERE id = @id AND owner_key_id = @ownerKeyId;
            """,
            new { id, ownerKeyId },
            cancellationToken: cancellationToken));
    }

    public async Task<Note> CreateAsync(string ownerKeyId, NoteRequest request, CancellationToken cancellationToken)
    {
        // Inserta y relee la fila en un solo round-trip para devolver los
        // timestamps generados por la BD.
        await using var connection = _dataSource.CreateConnection();
        return await connection.QuerySingleAsync<Note>(new CommandDefinition(
            """
            INSERT INTO notes (owner_key_id, title, text) VALUES (@ownerKeyId, @Title, @Text);
            SELECT id, title, text, created_at, updated_at FROM notes WHERE id = LAST_INSERT_ID();
            """,
            new { ownerKeyId, request.Title, request.Text },
            cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(
        string ownerKeyId, int id, NoteRequest request, CancellationToken cancellationToken)
    {
        await using var connection = _dataSource.CreateConnection();
        var rows = await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE notes SET title = @Title, text = @Text
            WHERE id = @id AND owner_key_id = @ownerKeyId;
            """,
            new { request.Title, request.Text, id, ownerKeyId },
            cancellationToken: cancellationToken));
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(string ownerKeyId, int id, CancellationToken cancellationToken)
    {
        await using var connection = _dataSource.CreateConnection();
        var rows = await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM notes WHERE id = @id AND owner_key_id = @ownerKeyId;",
            new { id, ownerKeyId },
            cancellationToken: cancellationToken));
        return rows > 0;
    }
}
