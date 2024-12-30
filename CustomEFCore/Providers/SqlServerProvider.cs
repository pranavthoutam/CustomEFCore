using Microsoft.Data.SqlClient;
using System.Text;

namespace CustomEFCore.Providers
{
    public class SqlServerProvider
    {
        private string _connectionString;

        public SqlServerProvider(string connectionString)
        {
            _connectionString = connectionString;
        }

        // Ensure the database exists or create it if it doesn't
        public void EnsureDatabaseCreated()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();

                    // Get the database name from the connection string
                    var dbName = new SqlConnectionStringBuilder(_connectionString).InitialCatalog;

                    // Check if the database exists
                    var checkDbQuery = $@"
                        IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = N'{dbName}')
                        BEGIN 
                            CREATE DATABASE {dbName};
                        END";

                    var command = new SqlCommand(checkDbQuery, connection);
                    command.ExecuteNonQuery();
                    Console.WriteLine($"Database '{dbName}' created or already exists.");
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"Error creating or accessing database: {ex.Message}");
                    throw;
                }
            }
        }

        // Switch to the target database in the connection string
        public void SwitchToTargetDatabase()
        {
            // Change the connection string to use the 'CustomEfCore' database after creation
            var builder = new SqlConnectionStringBuilder(_connectionString)
            {
                InitialCatalog = "CustomEfCore" // Change this to your actual target database name
            };

            _connectionString = builder.ToString();
            Console.WriteLine("Switched to target database: CustomEfCore.");
        }

        // Create tables based on model types (like Person)
        public void CreateTablesFromModel(IEnumerable<Type> entityTypes)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                foreach (var entityType in entityTypes)
                {
                    var tableName = entityType.Name;
                    var columns = GetColumns(entityType);
                    var createTableCommand = BuildCreateTableSql(tableName, columns);

                    var command = new SqlCommand(createTableCommand, connection);
                    command.ExecuteNonQuery();
                    Console.WriteLine($"Table '{tableName}' created or already exists.");
                }
            }
        }

        // Generate SQL for creating a table
        private string BuildCreateTableSql(string tableName, Dictionary<string, string> columns)
        {
            var sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine($"CREATE TABLE {tableName} (");

            foreach (var column in columns)
            {
                sqlBuilder.AppendLine($"    {column.Key} {column.Value},");
            }

            // Remove trailing comma
            sqlBuilder.Length -= 3;
            sqlBuilder.AppendLine(");");

            return sqlBuilder.ToString();
        }

        // Get columns for an entity using reflection
        private Dictionary<string, string> GetColumns(Type entityType)
        {
            var columns = new Dictionary<string, string>();

            foreach (var prop in entityType.GetProperties())
            {
                var columnType = GetSqlType(prop.PropertyType);
                columns.Add(prop.Name, columnType);
            }

            return columns;
        }

        // Map C# types to SQL Server types
        private string GetSqlType(Type type)
        {
            if (type == typeof(int)) return "INT";
            if (type == typeof(string)) return "NVARCHAR(MAX)";
            if (type == typeof(DateTime)) return "DATETIME";
            if (type == typeof(decimal)) return "DECIMAL";
            if (type == typeof(bool)) return "BIT";
            if (type == typeof(Guid)) return "UNIQUEIDENTIFIER";

            throw new Exception($"Unsupported type {type.Name}");
        }

        // Run migrations (basic version)
        public void ApplyMigrations(List<string> migrationScripts)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                foreach (var script in migrationScripts)
                {
                    var command = new SqlCommand(script, connection);
                    command.ExecuteNonQuery();
                    Console.WriteLine("Migration applied.");
                }
            }
        }
    }
}
