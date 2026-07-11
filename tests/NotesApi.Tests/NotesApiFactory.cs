using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MySqlConnector;
using NotesApi.Migrations;
using Testcontainers.MySql;

namespace NotesApi.Tests;

/// <summary>
/// Levanta un MySQL efímero (Testcontainers), le aplica las migraciones reales
/// y arranca la API apuntando a él. Toda la suite comparte una instancia.
/// </summary>
public class NotesApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string TestKeyId = "test-client";
    public const string TestSecret = "test-secret";
    public static string ValidApiKey => $"{TestKeyId}.{TestSecret}";

    private readonly MySqlContainer _mysql = new MySqlBuilder("mysql:8.4")
        .WithDatabase("notes")
        .WithUsername("notes_user")
        .WithPassword("notes_password")
        .Build();

    public async Task InitializeAsync()
    {
        await _mysql.StartAsync();

        if (!MigrationRunner.Run(_mysql.GetConnectionString(), includeSeeds: false))
        {
            throw new InvalidOperationException("No se pudieron aplicar las migraciones en la BD de pruebas.");
        }

        await CreateApiKeyAsync(TestKeyId, TestSecret);
    }

    /// <summary>Inserta una API key directamente en la BD de pruebas.</summary>
    public async Task CreateApiKeyAsync(string keyId, string secret, DateTime? expiresAt = null)
    {
        var keyHash = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        await using var connection = new MySqlConnection(_mysql.GetConnectionString());
        await connection.ExecuteAsync(
            """
            INSERT INTO api_keys (key_id, key_hash, client_name, expires_at)
            VALUES (@keyId, @keyHash, @keyId, @expiresAt);
            """,
            new { keyId, keyHash, expiresAt });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:NotesDb", _mysql.GetConnectionString());
        // El rate limiting se prueba aparte; aquí no debe interferir.
        builder.UseSetting("RateLimiting:PermitLimit", "10000");
    }

    public HttpClient CreateClientWithApiKey(string apiKey = "")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey.Length > 0 ? apiKey : ValidApiKey);
        return client;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _mysql.DisposeAsync();
    }
}
