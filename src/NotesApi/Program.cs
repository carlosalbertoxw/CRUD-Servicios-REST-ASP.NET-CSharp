using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using NotesApi.Auth;
using NotesApi.Data;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Las columnas snake_case (key_id, client_name) mapean a propiedades PascalCase.
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

// Las notas están limitadas a 100.000 caracteres; no hay razón para aceptar
// cuerpos mayores a 1 MB.
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 1_048_576);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();

builder.Services.AddOpenApi(options =>
{
    // Declara el esquema de API key para que Scalar permita configurarla.
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[ApiKeyAuthenticationHandler.SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = ApiKeyAuthenticationHandler.HeaderName,
            Description = "API key con formato <key_id>.<secreto>",
        };
        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(ApiKeyAuthenticationHandler.SchemeName, document)] = [],
        });
        return Task.CompletedTask;
    });
});

builder.Services.AddOptions<DatabaseOptions>()
    .BindConfiguration(DatabaseOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddScoped<INoteRepository, MySqlNoteRepository>();

// Las keys validadas se cachean un TTL corto: el caso caliente autentica sin
// tocar la BD. Una revocación tarda hasta ese TTL en surtir efecto.
builder.Services.AddMemoryCache();
builder.Services.AddScoped<MySqlApiKeyRepository>();
var keyCacheSeconds = builder.Configuration.GetValue("Authentication:KeyCacheSeconds", 60);
builder.Services.AddScoped<IApiKeyRepository>(sp => new CachedApiKeyRepository(
    sp.GetRequiredService<MySqlApiKeyRepository>(),
    sp.GetRequiredService<IMemoryCache>(),
    TimeSpan.FromSeconds(keyCacheSeconds)));

builder.Services.AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationHandler.SchemeName, null);

// Fail-closed: TODO endpoint exige autenticación salvo AllowAnonymous explícito.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var permitLimit = builder.Configuration.GetValue("RateLimiting:PermitLimit", 100);
var windowSeconds = builder.Configuration.GetValue("RateLimiting:WindowSeconds", 60);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = windowSeconds.ToString();
        return ValueTask.CompletedTask;
    };
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = 0,
            }));
});

builder.Services.AddHealthChecks()
    .AddMySql(sp => sp.GetRequiredService<IOptions<DatabaseOptions>>().Value.NotesDb, name: "mysql");

var app = builder.Build();

// Todos los errores (500 incluidos) responden ProblemDetails, sin detalles internos.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment())
{
    // Despliegue típico detrás de reverse proxy que termina TLS. Los proxies
    // confiables se declaran por configuración; sin ellos, ASP.NET Core solo
    // acepta X-Forwarded-* desde loopback y los descartaría detrás de un LB
    // real (bucle de redirección HTTPS y rate limiting por la IP del proxy).
    var forwardedOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    };
    foreach (var proxy in app.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
    {
        forwardedOptions.KnownProxies.Add(IPAddress.Parse(proxy));
    }
    foreach (var network in app.Configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>() ?? [])
    {
        forwardedOptions.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
    }
    app.UseForwardedHeaders(forwardedOptions);
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Documentación y health checks: únicos endpoints exentos de API key.
app.MapOpenApi().AllowAnonymous();
app.MapScalarApiReference().AllowAnonymous();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
app.MapHealthChecks("/health/ready").AllowAnonymous();

app.MapControllers();

app.Run();

// Expone la clase Program a las pruebas de integración (WebApplicationFactory).
public partial class Program;
