using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using NotesApi.Data;

namespace NotesApi.Auth;

/// <summary>
/// Esquema de autenticación por API key integrado al pipeline de ASP.NET Core.
/// El cliente envía "X-Api-Key: &lt;key_id&gt;.&lt;secreto&gt;"; se busca la key activa
/// por key_id y se compara el SHA-256 del secreto en tiempo constante.
/// Al ser un esquema con FallbackPolicy, todo endpoint exige autenticación
/// salvo que se marque explícitamente con AllowAnonymous (fail-closed).
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ApiKey";
    public const string HeaderName = "X-Api-Key";
    public const string KeyIdClaim = "api_key_id";

    private readonly IApiKeyRepository _apiKeys;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApiKeyRepository apiKeys)
        : base(options, logger, encoder)
    {
        _apiKeys = apiKeys;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var headerValue))
        {
            return AuthenticateResult.NoResult();
        }

        var parts = headerValue.ToString().Split('.', 2);
        if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0)
        {
            return AuthenticateResult.Fail($"Formato inválido; se espera '{HeaderName}: <key_id>.<secreto>'.");
        }

        var apiKey = await _apiKeys.GetActiveKeyAsync(parts[0], Context.RequestAborted);
        if (apiKey is null || !SecretMatches(parts[1], apiKey.KeyHash))
        {
            Logger.LogInformation("Intento de autenticación fallido para key_id {KeyId}", parts[0]);
            return AuthenticateResult.Fail("API key inválida, revocada o expirada.");
        }

        try
        {
            await _apiKeys.RegisterUseAsync(apiKey.KeyId, Context.RequestAborted);
        }
        catch (Exception ex)
        {
            // Telemetría de mejor esfuerzo: no debe tumbar una autenticación válida.
            Logger.LogWarning(ex, "No se pudo registrar el uso de la key {KeyId}", apiKey.KeyId);
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, apiKey.ClientName), new Claim(KeyIdClaim, apiKey.KeyId)],
            SchemeName);
        return AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Headers.WWWAuthenticate = SchemeName;
        return base.HandleChallengeAsync(properties);
    }

    private static bool SecretMatches(string providedSecret, byte[] storedHash)
    {
        var providedHash = SHA256.HashData(Encoding.UTF8.GetBytes(providedSecret));
        return CryptographicOperations.FixedTimeEquals(providedHash, storedHash);
    }
}
