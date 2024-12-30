using Microsoft.Data.SqlClient;
using CustomEFCore.Attributes;

namespace CustomEFCore.Migrations
{
    public class MigrationManager
    {
        private readonly SchemaInfoProvider _schemaInfoProvider;
        private readonly ScriptGenerator _scriptGenerator;

        public MigrationManager(SchemaInfoProvider schemaInfoProvider, ScriptGenerator scriptGenerator)
        {
            _schemaInfoProvider = schemaInfoProvider;
            _scriptGenerator = scriptGenerator;
        }

        public Dictionary<string, List<string>> GetCurrentSchema()
        {
            return _schemaInfoProvider.GetCurrentSchema();
        }

        public Dictionary<string, List<string>> GetModelSchema()
        {
            var modelSchema = new Dictionary<string, List<string>>();

            foreach (var modelType in AppDomain.CurrentDomain.GetAssemblies()
                     .SelectMany(a => a.GetTypes())
                     .Where(t => t.GetCustomAttributes(typeof(DbModelAttribute), true).Any()))
            {
                var tableName = modelType.Name;
                var properties = modelType.GetProperties();

                foreach (var prop in properties)
                {
                    if (!modelSchema.ContainsKey(tableName))
                        modelSchema[tableName] = new List<string>();

                    modelSchema[tableName].Add(prop.Name);
                }
            }

            return modelSchema;
        }

        public IEnumerable<string> CompareSchemas(
            Dictionary<string, List<string>> currentSchema,
            Dictionary<string, List<string>> modelSchema)
        {
            var changes = new List<string>();

            foreach (var table in modelSchema)
            {
                if (!currentSchema.ContainsKey(table.Key))
                {
                    changes.Add($"CREATE TABLE {table.Key} ({string.Join(", ", table.Value.Select(c => $"{c} NVARCHAR(MAX)"))});");
                }
                else
                {
                    foreach (var column in table.Value)
                    {
                        if (!currentSchema[table.Key].Contains(column))
                        {
                            changes.Add($"ALTER TABLE {table.Key} ADD {column} NVARCHAR(MAX);");
                        }
                    }
                }
            }

            return changes;
        }

        public IEnumerable<string> GenerateScripts(IEnumerable<string> changes)
        {
            return _scriptGenerator.GenerateScripts(changes);
        }

        public void ExecuteScript(string script)
        {
            using (var connection = new SqlConnection(_schemaInfoProvider.ConnectionString))
            {
                connection.Open();
                var command = new SqlCommand(script, connection);
                command.ExecuteNonQuery();
            }
        }
    }
}
