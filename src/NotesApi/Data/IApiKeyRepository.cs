using NotesApi.Auth;

namespace NotesApi.Data;

public interface IApiKeyRepository
{
    /// <summary>Devuelve la key activa (no revocada ni expirada) con ese id, o null.</summary>
    Task<ApiKey?> GetActiveKeyAsync(string keyId, CancellationToken cancellationToken);

    /// <summary>Registra el uso de la key (actualización laxa de last_used_at).</summary>
    Task RegisterUseAsync(string keyId, CancellationToken cancellationToken);
}
