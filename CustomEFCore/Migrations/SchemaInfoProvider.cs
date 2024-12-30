
using Microsoft.Data.SqlClient;

namespace CustomEFCore.Migrations
{
    public class SchemaInfoProvider
    {
        public string ConnectionString { get; }

        public SchemaInfoProvider(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

            ConnectionString = connectionString;
        }

        public List<string> GetExistingTables()
        {
            var tables = new List<string>();

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                var query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
                using (var command = new SqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tables.Add(reader.GetString(0));
                    }
                }
            }

            return tables;
        }

        public Dictionary<string, List<string>> GetCurrentSchema()
        {
            var schema = new Dictionary<string, List<string>>();

            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                var command = new SqlCommand(
                    "SELECT TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS",
                    connection
                );
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var table = reader.GetString(0);
                        var column = reader.GetString(1);

                        if (!schema.ContainsKey(table))
                        {
                            schema[table] = new List<string>();
                        }
                        schema[table].Add(column);
                    }
                }
            }

            return schema;
        }
    }
}
