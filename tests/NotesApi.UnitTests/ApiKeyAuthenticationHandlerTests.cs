using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NotesApi.Auth;

namespace NotesApi.UnitTests;

public class ApiKeyAuthenticationHandlerTests
{
    private const string KeyId = "cliente";
    private const string Secret = "secreto";
    private const string ClientName = "Cliente de pruebas";

    private static ApiKey CreateApiKey() => new()
    {
        KeyId = KeyId,
        KeyHash = SHA256.HashData(Encoding.UTF8.GetBytes(Secret)),
        ClientName = ClientName,
    };

    private static async Task<AuthenticateResult> AuthenticateAsync(
        FakeApiKeyRepository repository, string? headerValue)
    {
        var handler = new ApiKeyAuthenticationHandler(
            new TestOptionsMonitor(), NullLoggerFactory.Instance, UrlEncoder.Default, repository);

        var context = new DefaultHttpContext();
        if (headerValue is not null)
        {
            context.Request.Headers[ApiKeyAuthenticationHandler.HeaderName] = headerValue;
        }

        await handler.InitializeAsync(
            new AuthenticationScheme(
                ApiKeyAuthenticationHandler.SchemeName, null, typeof(ApiKeyAuthenticationHandler)),
            context);
        return await handler.AuthenticateAsync();
    }

    // T001
    [Fact]
    public async Task KeyValida_DevuelveSuccessConClaims()
    {
        var repository = new FakeApiKeyRepository { Key = CreateApiKey() };

        var result = await AuthenticateAsync(repository, $"{KeyId}.{Secret}");

        Assert.True(result.Succeeded);
        var principal = result.Ticket!.Principal;
        Assert.Equal(ClientName, principal.FindFirstValue(ClaimTypes.Name));
        Assert.Equal(KeyId, principal.FindFirstValue(ApiKeyAuthenticationHandler.KeyIdClaim));
    }

    // T002
    [Fact]
    public async Task SinHeader_DevuelveNoResult()
    {
        var repository = new FakeApiKeyRepository { Key = CreateApiKey() };

        var result = await AuthenticateAsync(repository, headerValue: null);

        Assert.True(result.None);
        Assert.False(result.Succeeded);
        Assert.Equal(0, repository.GetActiveKeyCalls);
    }

    // T003
    [Fact]
    public async Task HeaderSinPunto_DevuelveFailSinConsultarRepositorio()
    {
        var repository = new FakeApiKeyRepository { Key = CreateApiKey() };

        var result = await AuthenticateAsync(repository, "sin-punto");

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
        Assert.Contains("Formato inválido", result.Failure.Message);
        Assert.Equal(0, repository.GetActiveKeyCalls);
    }

    // T004
    [Theory]
    [InlineData(".secreto")]
    [InlineData("cliente.")]
    [InlineData(".")]
    public async Task KeyIdOSecretoVacios_DevuelvenFail(string headerValue)
    {
        var repository = new FakeApiKeyRepository { Key = CreateApiKey() };

        var result = await AuthenticateAsync(repository, headerValue);

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
        Assert.Equal(0, repository.GetActiveKeyCalls);
    }

    // T005
    [Fact]
    public async Task KeyIdInexistente_DevuelveFail()
    {
        var repository = new FakeApiKeyRepository { Key = null };

        var result = await AuthenticateAsync(repository, $"desconocido.{Secret}");

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    // T006
    [Fact]
    public async Task SecretoIncorrecto_DevuelveFail()
    {
        var repository = new FakeApiKeyRepository { Key = CreateApiKey() };

        var result = await AuthenticateAsync(repository, $"{KeyId}.otro-secreto");

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    // T007
    [Fact]
    public async Task SiRegisterUseFalla_LaAutenticacionSigueSiendoSuccess()
    {
        var repository = new FakeApiKeyRepository { Key = CreateApiKey(), ThrowOnRegisterUse = true };

        var result = await AuthenticateAsync(repository, $"{KeyId}.{Secret}");

        Assert.True(result.Succeeded);
        Assert.Equal(1, repository.RegisterUseCalls);
    }

    private sealed class TestOptionsMonitor : IOptionsMonitor<AuthenticationSchemeOptions>
    {
        public AuthenticationSchemeOptions CurrentValue { get; } = new();
        public AuthenticationSchemeOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<AuthenticationSchemeOptions, string?> listener) => null;
    }
}
