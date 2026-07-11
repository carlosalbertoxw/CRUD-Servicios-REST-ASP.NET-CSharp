namespace NotesApi.Auth;

/// <summary>Registro de una API key en la tabla api_keys (el secreto solo se guarda hasheado).</summary>
public class ApiKey
{
    public string KeyId { get; set; } = string.Empty;
    public byte[] KeyHash { get; set; } = [];
    public string ClientName { get; set; } = string.Empty;
}
