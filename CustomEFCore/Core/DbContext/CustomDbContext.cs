using CustomEFCore.Providers;
using CustomEFCore.Models;

namespace CustomEFCore.Core.DbContext
{
    public class CustomDbContext : IDisposable
    {
        private readonly string _connectionString;
        private readonly SqlServerProvider _provider;
        private readonly Dictionary<Type, object> _sets = new Dictionary<Type, object>();

        public CustomDbContext(string connectionString)
        {
            _connectionString = connectionString;
            _provider = new SqlServerProvider(connectionString);
            _provider.EnsureDatabaseCreated(); // Ensure the database exists
        }

        // Set method to return a DbSet<TEntity>
        public DbSet<TEntity> Set<TEntity>() where TEntity : class
        {
            if (!_sets.ContainsKey(typeof(TEntity)))
            {
                _sets[typeof(TEntity)] = new DbSet<TEntity>(this);
            }
            return (DbSet<TEntity>)_sets[typeof(TEntity)];
        }

        // Create tables based on models (entities)
        public void CreateTables()
        {
            _provider.CreateTablesFromModel(new List<Type> { typeof(Person) }); // Add more types as needed
        }

        // Apply migrations (optional for future use)
        public void ApplyMigrations(List<string> migrationScripts)
        {
            _provider.ApplyMigrations(migrationScripts);
        }

        // Save changes (stub for now)
        public void SaveChanges()
        {
            Console.WriteLine("Changes saved.");
        }

        // Dispose of resources
        public void Dispose()
        {
            // Dispose resources if necessary
            Console.WriteLine("DbContext disposed.");
        }
    }
}
