using Microsoft.Data.SqlClient;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using CustomEFCore.Attributes;
using CustomEFCore.Core.DbContext;
namespace CustomEFCore.Providers
{
    public class SqlServerProvider
    {
        private readonly string _connectionString;

        public SqlServerProvider(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

            _connectionString = connectionString;
        }

        public void EnsureDatabaseCreated()
        {
            var dbName = new SqlConnectionStringBuilder(_connectionString).InitialCatalog;

            var createDbQuery = $@"
                IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = N'{dbName}')
                BEGIN
                    CREATE DATABASE [{dbName}];
                END";

            ExecuteNonQuery(createDbQuery);
            Console.WriteLine($"Database '{dbName}' created or already exists.");
        }

        public void CreateTableFromEntity(Type entityType, string tableName)
        {
            var properties = entityType.GetProperties();
            var columnDefinitions = new List<string>();
            var foreignKeys = new List<string>();
            var primaryKey = string.Empty;

            foreach (var property in properties)
            {
                string columnDefinition;

                if (property.Name == "Id")
                {
                    if (property.PropertyType == typeof(int) || property.PropertyType == typeof(long) || property.PropertyType == typeof(short) || property.PropertyType == typeof(byte))
                    {
                        columnDefinition = $"[{property.Name}] {GetSqlType(property.PropertyType)} IDENTITY(1,1)";
                    }
                    else if (property.PropertyType == typeof(Guid))
                    {
                        columnDefinition = $"[{property.Name}] UNIQUEIDENTIFIER DEFAULT NEWSEQUENTIALID()";
                    }
                    else
                    {
                        columnDefinition = $"[{property.Name}] {GetSqlType(property.PropertyType)}";
                    }
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
                        Console.WriteLine($"Table for {tableName} created successfully or already exists.");
                    }
                    catch (SqlException ex)
                    {
                        Console.WriteLine($"Error executing SQL: {ex.Message}");
                    }
                }
            }
        }
        public void DeleteTable(string tableName)
        {
            var query = $"DROP TABLE IF EXISTS [{tableName}]";

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }

            Console.WriteLine($"Table '{tableName}' deleted.");
        }
        public List<string> GetExistingTables()
        {
            var tables = new List<string>();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand(
                    "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';",
                    connection
                );

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
        public void InsertEntity<T>(T entity) where T : class, new()
        {
            var tableName = GetTableNameFromContext<T>();

            if (string.IsNullOrEmpty(tableName))
                throw new InvalidOperationException($"Unable to resolve table name for entity type {typeof(T).Name}.");

            var properties = typeof(T).GetProperties();
            var primaryKey = properties.FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));

            bool excludePrimaryKey = primaryKey != null &&
                                     (primaryKey.PropertyType == typeof(int) ||
                                      primaryKey.PropertyType == typeof(long) ||
                                      primaryKey.PropertyType == typeof(short) ||
                                      primaryKey.PropertyType == typeof(byte));

            var filteredProperties = excludePrimaryKey
                ? properties.Where(p => !p.Name.Equals(primaryKey.Name, StringComparison.OrdinalIgnoreCase)).ToList()
                : properties.ToList();

            if (!filteredProperties.Any())
                throw new InvalidOperationException($"No properties available to insert for entity type {typeof(T).Name}.");

            var columns = string.Join(", ", filteredProperties.Select(p => $"[{p.Name}]"));
            var values = string.Join(", ", filteredProperties.Select(p => $"@{p.Name}"));

            var insertQuery = $@"INSERT INTO [{tableName}] ({columns}) VALUES ({values});";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(insertQuery, connection))
            {
                foreach (var property in filteredProperties)
                {
                    command.Parameters.AddWithValue($"@{property.Name}", property.GetValue(entity) ?? DBNull.Value);
                }

                connection.Open();
                command.ExecuteNonQuery();
                Console.WriteLine($"Entity inserted into table '{tableName}'.");
            }
        }
        private string GetTableNameFromContext<T>() where T : class, new()
        {
            var dbContextType = typeof(ApplicationDbContext);
            var property = dbContextType.GetProperties()
                .FirstOrDefault(p => p.PropertyType.IsGenericType &&
                                     p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) &&
                                     p.PropertyType.GetGenericArguments()[0] == typeof(T));

            return property?.Name ?? typeof(T).Name;
        }
        public void DeleteEntity<T>(T entity) where T:class ,new()
        {
            var tableName = GetTableNameFromContext<T>();
            var keyProperty = typeof(T).GetProperties().First();
            var keyValue = keyProperty.GetValue(entity);

            var deleteQuery = $@"DELETE FROM [{tableName}] WHERE [{keyProperty.Name}] = @KeyValue;";

            ExecuteNonQuery(deleteQuery, ("@KeyValue", keyValue));
            Console.WriteLine($"Entity deleted from table '{tableName}'.");
        }

        public void UpdateEntity<T>(T entity) where T : class, new()
        {
            var tableName = GetTableNameFromContext<T>();
            var properties = typeof(T).GetProperties();

            var primaryKey = properties.FirstOrDefault(p =>
                Attribute.IsDefined(p, typeof(PrimaryKeyAttribute))||
                p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase));

            if (primaryKey == null)
            {
                throw new InvalidOperationException($"Entity {typeof(T).Name} must have a primary key.");
            }

            var primaryKeyValue = primaryKey.GetValue(entity);
            if (primaryKeyValue == null)
            {
                throw new InvalidOperationException($"Primary key '{primaryKey.Name}' value cannot be null.");
            }

            var nonDefaultProperties = properties
                .Where(p =>
                {
                    var value = p.GetValue(entity);
                    if (p.Name.Equals(primaryKey.Name, StringComparison.OrdinalIgnoreCase))
                        return false;
                    return value != null && !Equals(value, GetDefaultValue(p.PropertyType));
                })
                .ToList();

            if (!nonDefaultProperties.Any())
            {
                throw new InvalidOperationException("No properties to update. Provide at least one property with a non-default value.");
            }

            var setClause = string.Join(", ", nonDefaultProperties.Select(p => $"[{p.Name}] = @{p.Name}"));
            var updateQuery = $@"UPDATE [{tableName}] SET {setClause} WHERE [{primaryKey.Name}] = @{primaryKey.Name};";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(updateQuery, connection))
            {
                foreach (var property in nonDefaultProperties)
                {
                    command.Parameters.AddWithValue($"@{property.Name}", property.GetValue(entity) ?? DBNull.Value);
                }

                command.Parameters.AddWithValue($"@{primaryKey.Name}", primaryKeyValue);

                connection.Open();
                command.ExecuteNonQuery();
                Console.WriteLine($"Entity updated in table '{tableName}'.");
            }
        }
        private object GetDefaultValue(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }
        public List<T> GetEntities<T>() where T : class, new()
        {
            var entities = new List<T>();
            var tableName = typeof(T).Name;

            var selectQuery = $@"SELECT * FROM [{tableName}];";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(selectQuery, connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var entity = new T();
                        foreach (var property in typeof(T).GetProperties())
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
        private void ExecuteNonQuery(string query, params (string ParameterName, object Value)[] parameters)
        {
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(query, connection))
            {
                if (parameters != null)
                {
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.ParameterName, param.Value ?? DBNull.Value);
                    }
                }
                connection.Open();
                command.ExecuteNonQuery();
            }
        }
        private string GetSqlType(Type type)
        {
            return type switch
            {
                { } t when t == typeof(int) => "INT",
                { } t when t == typeof(string) => "NVARCHAR(MAX)",
                { } t when t == typeof(DateTime) => "DATETIME",
                { } t when t == typeof(decimal) => "DECIMAL",
                { } t when t == typeof(bool) => "BIT",
                { } t when t == typeof(Guid) => "UNIQUEIDENTIFIER",
                { } t when t == typeof(float) => "FLOAT",
                _ => throw new NotSupportedException($"Unsupported type: {type.Name}")
            };
        }
        private bool IsForeignKey(PropertyInfo property)
        {
            return property.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase) ||
                   property.GetCustomAttribute<FKAttribute>() != null;
        }
        private string GetForeignKeyTable(PropertyInfo property)
        {
            var foreignKeyAttr = property.GetCustomAttribute<FKAttribute>();
            if (foreignKeyAttr != null)
            {
                return foreignKeyAttr.ReferencedTable;
            }
            if (property.Name.EndsWith("Id") && !property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
            {
                var entityName = property.Name.Substring(0, property.Name.Length - 2);
                var relatedEntityType = Type.GetType(entityName) ?? AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase));
                if (relatedEntityType != null)
                {
                    var getTableNameMethod = GetType().GetMethod(nameof(GetTableNameFromContext))?.MakeGenericMethod(relatedEntityType);
                    return getTableNameMethod?.Invoke(this, null)?.ToString() ?? entityName;
                }
            }
            return null;
        }
        public void UpdateTableSchema(Type entityType, string tableName)
        {
            var existingColumns = GetTableSchema(tableName);

            var modelColumns = entityType.GetProperties()
                .ToDictionary(
                    p => p.Name,
                    p => GetCompleteSqlType(p.PropertyType, p.GetCustomAttributes(typeof(RequiredAttribute), true).Any())
                );

            foreach (var property in entityType.GetProperties())
            {
                var columnName = property.Name;

                if (columnName.Equals("Id", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (existingColumns.TryGetValue(columnName, out var existingColumnType))
                {
                    var modelColumnType = GetCompleteSqlType(property.PropertyType, property.GetCustomAttributes(typeof(RequiredAttribute), true).Any());

                    if (!existingColumnType.Equals(modelColumnType, StringComparison.OrdinalIgnoreCase))
                    {
                        var alterColumnQuery = $@"
                                  ALTER TABLE [{tableName}] 
                                  ALTER COLUMN [{columnName}] {modelColumnType}";

                        ExecuteNonQuery(alterColumnQuery);
                        Console.WriteLine($"Updated column '{columnName}' in table '{tableName}'.");
                    }
                }
                else
                {
                    var modelColumnType = GetCompleteSqlType(property.PropertyType, property.GetCustomAttributes(typeof(RequiredAttribute), true).Any());

                    if (IsForeignKey(property))
                    {
                        var foreignKeyTable = GetForeignKeyTable(property);
                        if (!string.IsNullOrEmpty(foreignKeyTable))
                        {
                            var addForeignKeyColumnQuery = $@"
                        ALTER TABLE [{tableName}] 
                        ADD [{columnName}] {modelColumnType},
                        CONSTRAINT FK_{tableName}_{columnName} FOREIGN KEY ([{columnName}]) 
                        REFERENCES [{foreignKeyTable}]([Id])";

                            ExecuteNonQuery(addForeignKeyColumnQuery);
                            Console.WriteLine($"Added column '{columnName}' with foreign key to '{foreignKeyTable}.Id' in table '{tableName}'.");
                        }
                        else
                        {
                            Console.WriteLine($"Warning: Could not resolve the foreign key table for property '{property.Name}'.");
                        }
                    }
                    else
                    {
                        var addColumnQuery = $@"
                            ALTER TABLE [{tableName}] 
                            ADD [{columnName}] {modelColumnType}";

                        ExecuteNonQuery(addColumnQuery);
                        Console.WriteLine($"Added column '{columnName}' to table '{tableName}'.");
                    }
                }
            }

            foreach (var existingColumn in existingColumns.Keys)
            {
                if (!modelColumns.ContainsKey(existingColumn) && !existingColumn.Equals("Id", StringComparison.OrdinalIgnoreCase))
                {
                    var foreignKeyConstraintName = GetForeignKeyConstraintName(tableName, existingColumn);
                    if (!string.IsNullOrEmpty(foreignKeyConstraintName))
                    {
                        var dropConstraintQuery = $@"
                                ALTER TABLE [{tableName}] 
                                DROP CONSTRAINT [{foreignKeyConstraintName}]";

                        ExecuteNonQuery(dropConstraintQuery);
                        Console.WriteLine($"Dropped foreign key constraint '{foreignKeyConstraintName}' on column '{existingColumn}'.");
                    }

                    var dropColumnQuery = $@"
                            ALTER TABLE [{tableName}] 
                            DROP COLUMN [{existingColumn}]";

                    ExecuteNonQuery(dropColumnQuery);
                    Console.WriteLine($"Dropped column '{existingColumn}' from table '{tableName}'.");
                }
            }
        }
        private string GetForeignKeyConstraintName(string tableName, string columnName)
        {
            var query = $@"
                    SELECT CONSTRAINT_NAME 
                    FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE 
                    WHERE TABLE_NAME = '{tableName}' 
                      AND COLUMN_NAME = '{columnName}'";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(query, connection))
            {
                connection.Open();
                return command.ExecuteScalar() as string;
            }
        }
        private Dictionary<string, string> GetTableSchema(string tableName)
        {
            var schema = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var query = $@"
                            SELECT 
                                COLUMN_NAME, 
                                DATA_TYPE, 
                                IS_NULLABLE, 
                                CHARACTER_MAXIMUM_LENGTH, 
                                NUMERIC_PRECISION, 
                                NUMERIC_SCALE 
                            FROM INFORMATION_SCHEMA.COLUMNS 
                            WHERE TABLE_NAME = '{tableName}'";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(query, connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var columnName = reader.GetString(0);
                        var dataType = reader.GetString(1);
                        var isNullable = reader.GetString(2).Equals("YES", StringComparison.OrdinalIgnoreCase) ? "NULL" : "NOT NULL";

                        int? maxLength = reader.IsDBNull(3) ? null : reader.GetInt32(3);
                        byte? precision = reader.IsDBNull(4) ? null : (byte?)reader.GetByte(4);
                        int? scale = reader.IsDBNull(5) ? null : reader.GetInt32(5);

                        schema[columnName] = GetCompleteSqlType(dataType, maxLength, precision, scale, isNullable);
                    }
                }
            }

            return schema;
        }
        private string GetCompleteSqlType(Type propertyType, bool isRequired)
        {
            if (Nullable.GetUnderlyingType(propertyType) != null)
            {
                propertyType = Nullable.GetUnderlyingType(propertyType)!;
                isRequired = false;
            }
            var sqlType = propertyType switch
            {
                Type t when t == typeof(string) => "nvarchar(max)",
                Type t when t == typeof(int) => "int",
                Type t when t == typeof(decimal) => "decimal(18,2)",
                Type t when t == typeof(double) => "float",
                Type t when t == typeof(float) => "real",
                Type t when t == typeof(bool) => "bit",
                Type t when t == typeof(DateTime) => "datetime",
                Type t when t == typeof(byte[]) => "varbinary(max)",
                Type t when t == typeof(Guid) => "uniqueidentifier",
                Type t when t == typeof(long) => "bigint",
                Type t when t == typeof(short) => "smallint",
                Type t when t == typeof(byte) => "tinyint",
                Type t when t == typeof(TimeSpan) => "time",
                Type t when t == typeof(DateTimeOffset) => "datetimeoffset",
                _ => throw new NotSupportedException($"Type {propertyType.Name} is not supported.")
            };

            return $"{sqlType} {(isRequired ? "NOT NULL" : "NULL")}";
        }
        private string GetCompleteSqlType(string dataType, int? charMaxLength, byte? precision, int? scale, string isNullable)
        {
            if (dataType.Equals("nvarchar", StringComparison.OrdinalIgnoreCase) ||
                dataType.Equals("varchar", StringComparison.OrdinalIgnoreCase) ||
                dataType.Equals("char", StringComparison.OrdinalIgnoreCase) ||
                dataType.Equals("nchar", StringComparison.OrdinalIgnoreCase))
            {
                if (charMaxLength.HasValue && charMaxLength.Value != -1)
                    return $"{dataType}({charMaxLength.Value}) {isNullable}";
                return $"{dataType}(max) {isNullable}";
            }

            if (dataType.Equals("decimal", StringComparison.OrdinalIgnoreCase) ||
                dataType.Equals("numeric", StringComparison.OrdinalIgnoreCase))
            {
                return $"{dataType}({precision ?? 18},{scale ?? 0}) {isNullable}";
            }

            if (dataType.Equals("varbinary", StringComparison.OrdinalIgnoreCase) ||
                dataType.Equals("binary", StringComparison.OrdinalIgnoreCase))
            {
                if (charMaxLength.HasValue && charMaxLength.Value != -1)
                    return $"{dataType}({charMaxLength.Value}) {isNullable}";
                return $"{dataType}(max) {isNullable}";
            }

            if (dataType.Equals("time", StringComparison.OrdinalIgnoreCase) ||
                dataType.Equals("datetime2", StringComparison.OrdinalIgnoreCase) ||
                dataType.Equals("datetimeoffset", StringComparison.OrdinalIgnoreCase))
            {
                return $"{dataType}({scale ?? 7}) {isNullable}";
            }

            return $"{dataType} {isNullable}";
        }
    }
}