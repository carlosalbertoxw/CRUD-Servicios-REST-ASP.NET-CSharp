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
        var assembly = Assembly.GetExecutingAssembly();

        var result = DeployChanges.To
            .MySqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(assembly, s => s.Contains(".Scripts."))
            .WithTransactionPerScript()
            .LogToConsole()
            .Build()
            .PerformUpgrade();

        if (!result.Successful)
        {
            Console.Error.WriteLine($"Error aplicando migraciones: {result.Error}");
            return false;
        }

        if (includeSeeds)
        {
            var seedResult = DeployChanges.To
                .MySqlDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(assembly, s => s.Contains(".Seeds."))
                .WithTransactionPerScript()
                .LogToConsole()
                .Build()
                .PerformUpgrade();

            if (!seedResult.Successful)
            {
                Console.Error.WriteLine($"Error aplicando datos de desarrollo: {seedResult.Error}");
                return false;
            }
        }

        return true;
    }
}
