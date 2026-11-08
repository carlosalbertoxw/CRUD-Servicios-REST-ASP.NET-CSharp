using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using NotesApi.Models;

namespace NotesApi.Tests;

public class NotesApiTests : IClassFixture<NotesApiFactory>
{
    private readonly NotesApiFactory _factory;

    /// <summary>Token de la prueba en curso: cancela las peticiones si xUnit aborta la prueba.</summary>
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public NotesApiTests(NotesApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_SinApiKey_Devuelve401()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/notes", Ct);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("formato-sin-punto")]
    [InlineData("test-client.secreto-incorrecto")]
    [InlineData("key-inexistente.test-secret")]
    public async Task Get_ConApiKeyInvalida_Devuelve401(string apiKey)
    {
        using var client = _factory.CreateClientWithApiKey(apiKey);
        var response = await client.GetAsync("/api/notes", Ct);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_ConKeyExpirada_Devuelve401()
    {
        await _factory.CreateApiKeyAsync("expired-client", "secreto", DateTime.UtcNow.AddHours(-1));
        using var client = _factory.CreateClientWithApiKey("expired-client.secreto");
        var response = await client.GetAsync("/api/notes", Ct);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CicloCrudCompleto()
    {
        using var client = _factory.CreateClientWithApiKey();

        // Crear
        var createResponse = await client.PostAsJsonAsync("/api/notes",
            new { title = "Nota de prueba", text = "Contenido con acentos: áéíóú ñ" }, Ct);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<Note>(Ct);
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.NotEqual(default, created.CreatedAt);
        Assert.Equal($"/api/notes/{created.Id}", createResponse.Headers.Location?.AbsolutePath);

        // Leer
        var fetched = await client.GetFromJsonAsync<Note>($"/api/notes/{created.Id}", Ct);
        Assert.NotNull(fetched);
        Assert.Equal("Nota de prueba", fetched.Title);
        Assert.Equal("Contenido con acentos: áéíóú ñ", fetched.Text);

        // Actualizar
        var updateResponse = await client.PutAsJsonAsync($"/api/notes/{created.Id}",
            new { title = "Título editado", text = "Contenido editado" }, Ct);
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
        var updated = await client.GetFromJsonAsync<Note>($"/api/notes/{created.Id}", Ct);
        Assert.Equal("Título editado", updated!.Title);

        // Eliminar
        var deleteResponse = await client.DeleteAsync($"/api/notes/{created.Id}", Ct);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        var afterDelete = await client.GetAsync($"/api/notes/{created.Id}", Ct);
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task Fechas_SeSerializanEnUtcConSufijoZ()
    {
        using var client = _factory.CreateClientWithApiKey();
        var create = await client.PostAsJsonAsync("/api/notes", new { title = "Nota con fecha" }, Ct);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        using var document = JsonDocument.Parse(await create.Content.ReadAsStringAsync(Ct));
        Assert.EndsWith("Z", document.RootElement.GetProperty("createdAt").GetString());
        Assert.EndsWith("Z", document.RootElement.GetProperty("updatedAt").GetString());
    }

    [Fact]
    public async Task Respuestas_IncluyenNoSniff()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/live", Ct);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [Fact]
    public async Task NotasDeOtroCliente_NoSonVisiblesNiModificables()
    {
        await _factory.CreateApiKeyAsync("otro-cliente", "otro-secreto");
        using var owner = _factory.CreateClientWithApiKey();
        using var intruder = _factory.CreateClientWithApiKey("otro-cliente.otro-secreto");

        var create = await owner.PostAsJsonAsync("/api/notes",
            new { title = "Nota privada", text = "solo del dueño" }, Ct);
        var note = await create.Content.ReadFromJsonAsync<Note>(Ct);

        // El otro cliente no puede verla, editarla ni borrarla (404, no 403:
        // no se revela que el recurso existe).
        Assert.Equal(HttpStatusCode.NotFound,
            (await intruder.GetAsync($"/api/notes/{note!.Id}", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await intruder.PutAsJsonAsync($"/api/notes/{note.Id}",
            new { title = "hackeada", text = "x" }, Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await intruder.DeleteAsync($"/api/notes/{note.Id}", Ct)).StatusCode);

        // Y su listado no la incluye.
        var listJson = await intruder.GetStringAsync("/api/notes?pageSize=100", Ct);
        using var document = JsonDocument.Parse(listJson);
        foreach (var item in document.RootElement.GetProperty("items").EnumerateArray())
        {
            Assert.NotEqual(note.Id, item.GetProperty("id").GetInt32());
        }

        // El dueño sigue viéndola intacta.
        var intact = await owner.GetFromJsonAsync<Note>($"/api/notes/{note.Id}", Ct);
        Assert.Equal("Nota privada", intact!.Title);
    }

    [Fact]
    public async Task Post_SinTitulo_Devuelve400()
    {
        using var client = _factory.CreateClientWithApiKey();
        var response = await client.PostAsJsonAsync("/api/notes", new { text = "sin título" }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_ConTextoExcesivo_Devuelve400()
    {
        using var client = _factory.CreateClientWithApiKey();
        var response = await client.PostAsJsonAsync("/api/notes",
            new { title = "Nota enorme", text = new string('x', 100_001) }, Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Listado_EsPaginadoPorKeyset_YNoIncluyeElTexto()
    {
        using var client = _factory.CreateClientWithApiKey();
        for (var i = 1; i <= 3; i++)
        {
            var create = await client.PostAsJsonAsync("/api/notes",
                new { title = $"Nota paginada {i}", text = "contenido largo" }, Ct);
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        }

        // Primera página
        var firstJson = await client.GetStringAsync("/api/notes?pageSize=2", Ct);
        using var first = JsonDocument.Parse(firstJson);
        var firstRoot = first.RootElement;

        Assert.Equal(2, firstRoot.GetProperty("items").GetArrayLength());
        Assert.Equal(2, firstRoot.GetProperty("pageSize").GetInt32());
        Assert.False(firstRoot.TryGetProperty("totalCount", out _),
            "El listado no calcula un total: haría un COUNT(*) en cada página.");
        Assert.Equal(JsonValueKind.Number, firstRoot.GetProperty("nextAfterId").ValueKind);

        var firstIds = firstRoot.GetProperty("items").EnumerateArray()
            .Select(i => i.GetProperty("id").GetInt32()).ToHashSet();
        foreach (var item in firstRoot.GetProperty("items").EnumerateArray())
        {
            Assert.False(item.TryGetProperty("text", out _),
                "El listado no debe incluir el contenido completo de las notas.");
        }

        // Segunda página vía cursor: sin traslape con la primera.
        var afterId = firstRoot.GetProperty("nextAfterId").GetInt32();
        var secondJson = await client.GetStringAsync($"/api/notes?pageSize=2&afterId={afterId}", Ct);
        using var second = JsonDocument.Parse(secondJson);
        foreach (var item in second.RootElement.GetProperty("items").EnumerateArray())
        {
            Assert.DoesNotContain(item.GetProperty("id").GetInt32(), firstIds);
        }
    }

    [Fact]
    public async Task Listado_ConBusqueda_FiltraPorTextoCompleto()
    {
        using var client = _factory.CreateClientWithApiKey();
        var create = await client.PostAsJsonAsync("/api/notes",
            new { title = "Receta secreta", text = "zanahoria con jengibre" }, Ct);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var json = await client.GetStringAsync("/api/notes?search=zanahoria", Ct);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var items = root.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal("Receta secreta", items[0].GetProperty("title").GetString());
    }

    [Fact]
    public async Task Listado_ConPageSizeInvalido_Devuelve400()
    {
        using var client = _factory.CreateClientWithApiKey();
        var response = await client.GetAsync("/api/notes?pageSize=500", Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_IdInexistente_Devuelve404()
    {
        using var client = _factory.CreateClientWithApiKey();
        var response = await client.GetAsync("/api/notes/999999", Ct);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task HealthChecks_SonAnonimos_YResponden200()
    {
        using var client = _factory.CreateClient();
        var live = await client.GetAsync("/health/live", Ct);
        var ready = await client.GetAsync("/health/ready", Ct);
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
    }
}
