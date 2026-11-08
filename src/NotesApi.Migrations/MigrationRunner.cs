using System.Reflection;
using DbUp;

namespace NotesApi.Migrations;

/// <summary>
/// Ejecuta las migraciones embebidas contra la base de datos indicada.
/// Público para que las pruebas de integración puedan preparar su propia BD.
/// </summary>
public static class MigrationRunner
{
    public static bool Run(string connectionString, bool includeSeeds)
    {
        return Apply(connectionString, ".Scripts.", "Error aplicando migraciones")
            && (!includeSeeds || Apply(connectionString, ".Seeds.", "Error aplicando datos de desarrollo"));
    }

    private static bool Apply(string connectionString, string resourceFolder, string errorMessage)
    {
        var result = DeployChanges.To
            .MySqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(), s => s.Contains(resourceFolder))
            .WithTransactionPerScript()
            .LogToConsole()
            .Build()
            .PerformUpgrade();

        if (!result.Successful)
        {
            Console.Error.WriteLine($"{errorMessage}: {result.Error}");
        }

        return result.Successful;
    }
}
