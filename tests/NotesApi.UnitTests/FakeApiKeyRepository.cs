using NotesApi.Auth;
using NotesApi.Data;

namespace NotesApi.UnitTests;

/// <summary>
/// Doble de IApiKeyRepository controlable desde los tests: devuelve la key
/// configurada, cuenta las llamadas y puede simular fallos en RegisterUseAsync.
/// </summary>
public sealed class FakeApiKeyRepository : IApiKeyRepository
{
    public ApiKey? Key { get; set; }
    public bool ThrowOnRegisterUse { get; set; }

    public int GetActiveKeyCalls { get; private set; }
    public int RegisterUseCalls { get; private set; }

    public Task<ApiKey?> GetActiveKeyAsync(string keyId, CancellationToken cancellationToken)
    {
        GetActiveKeyCalls++;
        return Task.FromResult(Key is not null && Key.KeyId == keyId ? Key : null);
    }

    public Task RegisterUseAsync(string keyId, CancellationToken cancellationToken)
    {
        RegisterUseCalls++;
        return ThrowOnRegisterUse
            ? Task.FromException(new InvalidOperationException("Fallo simulado al registrar el uso."))
            : Task.CompletedTask;
    }
}
