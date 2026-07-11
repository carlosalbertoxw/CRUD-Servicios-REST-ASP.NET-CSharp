using NotesApi.Migrations;

// Cadena de conexión: --connection "<cs>", variable de entorno
// ConnectionStrings__NotesDb, o el valor de desarrollo local por defecto.
const string DefaultDevConnection =
    "Server=localhost;Port=3306;Database=notes;User ID=notes_user;Password=notes_password";

string? FromArgs()
{
    var index = Array.IndexOf(args, "--connection");
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

var connectionString = FromArgs()
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__NotesDb")
    ?? DefaultDevConnection;

var includeSeeds = args.Contains("--seed");

Console.WriteLine(includeSeeds
    ? "Aplicando migraciones y datos de desarrollo (--seed)..."
    : "Aplicando migraciones...");

return MigrationRunner.Run(connectionString, includeSeeds) ? 0 : 1;
