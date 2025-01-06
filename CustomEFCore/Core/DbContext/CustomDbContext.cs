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

            DeleteUnmentionedTables();
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
        private void DeleteUnmentionedTables()
        {
            var modelNames = GetModelNames().Select(name => name.ToLowerInvariant().TrimEnd('s')).ToList();
            var dbSetNames = GetDbSetNames().Select(name => name.ToLowerInvariant().TrimEnd('s')).ToList();
            var dbTables = _existingTables.Select(name => name.ToLowerInvariant().TrimEnd('s')).ToList();

            var tablesToDelete = dbTables
                .Where(table => modelNames.Contains(table.ToLowerInvariant().TrimEnd('s')) &&
                                !dbSetNames.Contains(table.ToLowerInvariant().TrimEnd('s')))
                .ToList();

            foreach (var table in tablesToDelete)
            {
                Console.WriteLine($"Deleting unmentioned table: {table}");
                _provider.DeleteTable(table);
            }

            if (tablesToDelete.Any())
            {
                Console.WriteLine($"Deleted tables: {string.Join(", ", tablesToDelete)}");
            }
            else
            {
                Console.WriteLine("No unmentioned tables to delete.");
            }
        }
        private List<string> GetDbSetNames()
        {
            return GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                            .Where(p => p.PropertyType.IsGenericType &&
                                        p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                            .Select(p => p.Name)
                            .ToList();
        }

        private List<string> GetModelNames()
        {
            var modelsNamespace = "CustomEFCore.Models";
            return Assembly.GetExecutingAssembly().GetTypes()
                           .Where(t => t.Namespace == modelsNamespace && t.IsClass)
                           .Select(t => t.Name)
                           .ToList();
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
