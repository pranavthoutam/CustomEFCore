using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace CustomEFCore.Providers
{
    public class SqlServerProvider
    {
        private readonly string _connectionString;

        public SqlServerProvider(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void EnsureDatabaseCreated()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();

                    var dbName = new SqlConnectionStringBuilder(_connectionString).InitialCatalog;
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

        public void CreateTableFromEntity(Type entityType)
        {
            var tableName = entityType.Name;
            var properties = entityType.GetProperties();
            var columnDefinitions = new List<string>();
            var foreignKeys = new List<string>();
            var primaryKey = string.Empty;

            foreach (var property in properties)
            {
                string columnDefinition;

                // Handle primary key
                if (property.Name == "Id")
                {
                    columnDefinition = $"[{property.Name}] INT";
                    primaryKey = $"PRIMARY KEY ([{property.Name}])";
                }
                else
                {
                    columnDefinition = $"[{property.Name}] {GetSqlType(property.PropertyType)}";
                }

                columnDefinitions.Add(columnDefinition);

                if (IsForeignKey(property))
                {
                    var foreignKeyTable = GetForeignKeyTable(property);
                    if (foreignKeyTable != null)
                    {
                        var foreignKeyDefinition = $"CONSTRAINT FK_{tableName}_{property.Name} FOREIGN KEY ([{property.Name}]) REFERENCES [{foreignKeyTable}]([Id])";
                        foreignKeys.Add(foreignKeyDefinition);
                    }
                }
            }

            var columns = string.Join(", ", columnDefinitions);
            if (!string.IsNullOrEmpty(primaryKey))
            {
                columns += $", {primaryKey}";
            }

            var foreignKeysClause = foreignKeys.Count > 0 ? ", " + string.Join(", ", foreignKeys) : string.Empty;

            var createTableQuery = $"CREATE TABLE [{tableName}] ({columns}{foreignKeysClause})";

            Console.WriteLine($"Executing Query: {createTableQuery}");

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(createTableQuery, connection))
                {
                    try
                    {
                        command.ExecuteNonQuery();
                        Console.WriteLine($"Table for {tableName} created successfully.");
                    }
                    catch (SqlException ex)
                    {
                        Console.WriteLine($"Error executing SQL: {ex.Message}");
                    }
                }
            }
        }


        private string GetForeignKeyTable(PropertyInfo property)
        {
            if (property.Name.EndsWith("Id") && property.Name !="Id")
            {
                return property.Name.Replace("Id", "");
            }
            var foreignKeyAttr = property.GetCustomAttribute<ForeignKeyAttribute>();
            return foreignKeyAttr?.Name;
        }



        private bool IsForeignKey(PropertyInfo property)
        {
            return property.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase) ||
                   property.GetCustomAttribute<ForeignKeyAttribute>() != null;
        }


        private Type GetReferencedEntity(PropertyInfo property)
        {
            var referencedTypeName = property.Name.Substring(0, property.Name.Length - 2);
            return Type.GetType($"CustomEFCore.Models.{referencedTypeName}, CustomEFCore");
        }

        public void InsertEntity<TEntity>(TEntity entity)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var tableName = typeof(TEntity).Name;
                var properties = typeof(TEntity).GetProperties();
                var columns = string.Join(",", properties.Select(p => p.Name));
                var values = string.Join(",", properties.Select(p => $"'{p.GetValue(entity)}'"));

                var insertQuery = $"INSERT INTO {tableName} ({columns}) VALUES ({values});";

                using (var command = new SqlCommand(insertQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
                Console.WriteLine($"Entity inserted into table '{tableName}'.");
            }
        }


        public void DeleteEntity<TEntity>(TEntity entity)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var tableName = typeof(TEntity).Name;
                var keyProperty = typeof(TEntity).GetProperties().First();
                var keyName = keyProperty.Name;
                var keyValue = keyProperty.GetValue(entity);

                var deleteQuery = $"DELETE FROM {tableName} WHERE {keyName} = '{keyValue}';";

                using (var command = new SqlCommand(deleteQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
                Console.WriteLine($"Entity deleted from table '{tableName}'.");
            }
        }

        public List<TEntity> GetEntities<TEntity>() where TEntity : class
        {
            var entities = new List<TEntity>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                var tableName = typeof(TEntity).Name;
                var selectQuery = $"SELECT * FROM {tableName};";

                using (var command = new SqlCommand(selectQuery, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var entity = Activator.CreateInstance<TEntity>();
                        foreach (var property in typeof(TEntity).GetProperties())
                        {
                            var value = reader[property.Name];
                            property.SetValue(entity, value == DBNull.Value ? null : value);
                        }
                        entities.Add(entity);
                    }
                }
            }

            return entities;
        }

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
    }
}
