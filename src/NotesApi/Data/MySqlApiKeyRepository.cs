using Dapper;
using MySqlConnector;
using NotesApi.Auth;

namespace NotesApi.Data;

public class MySqlApiKeyRepository : IApiKeyRepository
{
    private readonly MySqlDataSource _dataSource;

    public MySqlApiKeyRepository(MySqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<ApiKey?> GetActiveKeyAsync(string keyId, CancellationToken cancellationToken)
    {
        await using var connection = _dataSource.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<ApiKey>(new CommandDefinition(
            """
            SELECT key_id, key_hash, client_name
            FROM api_keys
            WHERE key_id = @keyId
              AND revoked_at IS NULL
              AND (expires_at IS NULL OR expires_at > UTC_TIMESTAMP());
            """,
            new { keyId },
            cancellationToken: cancellationToken));
    }

    public async Task RegisterUseAsync(string keyId, CancellationToken cancellationToken)
    {
        // Actualización laxa: solo escribe si pasó más de una hora desde el
        // último registro, para no convertir cada request en una escritura.
        await using var connection = _dataSource.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE api_keys
            SET last_used_at = UTC_TIMESTAMP()
            WHERE key_id = @keyId
              AND (last_used_at IS NULL OR last_used_at < UTC_TIMESTAMP() - INTERVAL 1 HOUR);
            """,
            new { keyId },
            cancellationToken: cancellationToken));
    }
}
