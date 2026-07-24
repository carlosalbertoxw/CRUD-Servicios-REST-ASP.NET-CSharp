using Microsoft.Extensions.Caching.Memory;
using NotesApi.Auth;
using NotesApi.Data;

namespace NotesApi.UnitTests;

public class CachedApiKeyRepositoryTests
{
    private const string KeyId = "cliente";

    private static ApiKey CreateApiKey() => new()
    {
        KeyId = KeyId,
        KeyHash = [1, 2, 3],
        ClientName = "Cliente de pruebas",
    };

    private static CachedApiKeyRepository CreateSut(FakeApiKeyRepository inner, TimeSpan? ttl = null) =>
        new(inner, new MemoryCache(new MemoryCacheOptions()), ttl ?? TimeSpan.FromMinutes(5));

    // T008
    [Fact]
    public async Task PrimeraConsulta_DelegaAlRepositorioInterno()
    {
        var inner = new FakeApiKeyRepository { Key = CreateApiKey() };
        var sut = CreateSut(inner);

        var key = await sut.GetActiveKeyAsync(KeyId, CancellationToken.None);

        Assert.Same(inner.Key, key);
        Assert.Equal(1, inner.GetActiveKeyCalls);
    }

    // T009
    [Fact]
    public async Task SegundaConsultaDentroDelTtl_SaleDelCacheSinTocarAlInterno()
    {
        var inner = new FakeApiKeyRepository { Key = CreateApiKey() };
        var sut = CreateSut(inner);

        var first = await sut.GetActiveKeyAsync(KeyId, CancellationToken.None);
        var second = await sut.GetActiveKeyAsync(KeyId, CancellationToken.None);

        Assert.Same(first, second);
        Assert.Equal(1, inner.GetActiveKeyCalls);
    }

    // T010
    [Fact]
    public async Task KeyInexistente_ElNullTambienSeCachea()
    {
        var inner = new FakeApiKeyRepository { Key = null };
        var sut = CreateSut(inner);

        var first = await sut.GetActiveKeyAsync("inexistente", CancellationToken.None);
        var second = await sut.GetActiveKeyAsync("inexistente", CancellationToken.None);

        Assert.Null(first);
        Assert.Null(second);
        Assert.Equal(1, inner.GetActiveKeyCalls);
    }

    // T011
    [Fact]
    public async Task RegisterUse_SoloDelegaLaPrimeraVezDentroDeLaVentana()
    {
        var inner = new FakeApiKeyRepository { Key = CreateApiKey() };
        var sut = CreateSut(inner);

        await sut.RegisterUseAsync(KeyId, CancellationToken.None);
        await sut.RegisterUseAsync(KeyId, CancellationToken.None);

        Assert.Equal(1, inner.RegisterUseCalls);
    }

    // T012
    [Fact]
    public async Task ConTtlVencido_VuelveAConsultarAlInterno()
    {
        var inner = new FakeApiKeyRepository { Key = CreateApiKey() };
        var sut = CreateSut(inner, ttl: TimeSpan.FromMilliseconds(50));

        await sut.GetActiveKeyAsync(KeyId, CancellationToken.None);
        await Task.Delay(300);
        await sut.GetActiveKeyAsync(KeyId, CancellationToken.None);

        Assert.Equal(2, inner.GetActiveKeyCalls);
    }
}
