using CustomEFCore.Migrations;

namespace CustomEFCore.Core
{
    public class SchemaUpdater
    {
        private readonly MigrationManager _migrationManager;

        public SchemaUpdater(MigrationManager migrationManager)
        {
            _migrationManager = migrationManager;
        }

        public void UpdateSchema()
        {
            var currentSchema = _migrationManager.GetCurrentSchema();
            var modelSchema = _migrationManager.GetModelSchema();

            var changes = _migrationManager.CompareSchemas(currentSchema, modelSchema);

            if (!changes.Any())
            {
                Console.WriteLine("Database is up-to-date.");
                return;
            }

            Console.WriteLine("Updating database schema...");
            var scripts = _migrationManager.GenerateScripts(changes);

            foreach (var script in scripts)
            {
                Console.WriteLine($"Executing script: {script}");
                _migrationManager.ExecuteScript(script);
            }

            Console.WriteLine("Schema updated successfully.");
        }
    }
}
