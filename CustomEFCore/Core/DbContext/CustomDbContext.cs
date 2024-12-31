using CustomEFCore.Providers;
using System.Reflection;

namespace CustomEFCore.Core.DbContext
{
    public class CustomDbContext : IDisposable
    {
        private readonly string _connectionString;
        private readonly SqlServerProvider _provider;
        private readonly HashSet<string> _existingTables = new();

        public CustomDbContext(string connectionString)
        {
            _connectionString = connectionString;
            _provider = new SqlServerProvider(connectionString);

            _existingTables = new HashSet<string>(_provider.GetExistingTables(), StringComparer.OrdinalIgnoreCase);

            InitializeDbSets();
        }

        private void InitializeDbSets()
        {
            var dbSetProperties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                           .Where(p => p.PropertyType.IsGenericType &&
                                                       p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

            foreach (var property in dbSetProperties)
            {
                var entityType = property.PropertyType.GetGenericArguments()[0];
                var tableName = property.Name;

                EnsureTableCreated(entityType, tableName);

                var dbSetInstance = Activator.CreateInstance(typeof(DbSet<>).MakeGenericType(entityType), _provider);
                property.SetValue(this, dbSetInstance);
            }
        }

        private void EnsureTableCreated(Type entityType, string tableName)
        {
            if (!_existingTables.Contains(tableName))
            {
                Console.WriteLine($"Table '{tableName}' does not exist. Creating...");
                _provider.CreateTableFromEntity(entityType, tableName);
                _existingTables.Add(tableName);
            }
            else
            {
                Console.WriteLine($"Table '{tableName}' already exists. Updating schema if needed...");
                _provider.UpdateTableSchema(entityType, tableName);
            }
        }

        public void SaveChanges()
        {
            Console.WriteLine("Changes saved.");
        }

        public void Dispose()
        {
            Console.WriteLine("DbContext disposed.");
        }
    }
}
