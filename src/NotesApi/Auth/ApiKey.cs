namespace NotesApi.Auth;

/// <summary>Registro de una API key en la tabla api_keys (el secreto solo se guarda hasheado).</summary>
public sealed record ApiKey
{
    public string KeyId { get; init; } = string.Empty;
    public byte[] KeyHash { get; init; } = [];
    public string ClientName { get; init; } = string.Empty;
}
