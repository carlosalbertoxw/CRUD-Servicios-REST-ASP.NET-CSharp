using Microsoft.Extensions.Caching.Memory;
using NotesApi.Auth;

namespace NotesApi.Data;

/// <summary>
/// Decorador con cache en memoria sobre el repositorio de API keys: el caso
/// caliente autentica sin tocar la BD (ni lookup ni registro de uso), dejando
/// la petición en un solo round-trip. El costo asumido y documentado es que
/// una revocación tarda hasta el TTL del cache en surtir efecto.
/// </summary>
public class CachedApiKeyRepository : IApiKeyRepository
{
    private readonly IApiKeyRepository _inner;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _ttl;

    public CachedApiKeyRepository(IApiKeyRepository inner, IMemoryCache cache, TimeSpan ttl)
    {
        _inner = inner;
        _cache = cache;
        _ttl = ttl;
    }

    public async Task<ApiKey?> GetActiveKeyAsync(string keyId, CancellationToken cancellationToken)
    {
        var cacheKey = $"api-key:{keyId}";
        if (_cache.TryGetValue(cacheKey, out ApiKey? cached))
        {
            return cached;
        }

        var key = await _inner.GetActiveKeyAsync(keyId, cancellationToken);
        // También se cachea el "no existe" para no martillar la BD con key_ids inválidos.
        _cache.Set(cacheKey, key, _ttl);
        return key;
    }

    public async Task RegisterUseAsync(string keyId, CancellationToken cancellationToken)
    {
        var cacheKey = $"api-key-used:{keyId}";
        if (_cache.TryGetValue(cacheKey, out _))
        {
            return;
        }

        await _inner.RegisterUseAsync(keyId, cancellationToken);
        // Menor que el INTERVAL 1 HOUR del UPDATE: la BD sigue siendo la fuente
        // de verdad aunque haya varias réplicas con caches independientes.
        _cache.Set(cacheKey, true, TimeSpan.FromMinutes(50));
    }
}
