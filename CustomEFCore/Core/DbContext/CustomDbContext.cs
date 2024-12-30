using CustomEFCore.Providers;

namespace CustomEFCore.Core.DbContext
{
    public class CustomDbContext : IDisposable
    {
        private readonly string _connectionString;
        private readonly SqlServerProvider _provider;
        private readonly Dictionary<Type, object> _sets = new Dictionary<Type, object>();
        private readonly HashSet<Type> _initializedEntities = new HashSet<Type>();

        public CustomDbContext(string connectionString)
        {
            _connectionString = connectionString;
            _provider = new SqlServerProvider(connectionString);
            _provider.EnsureDatabaseCreated();
        }
        public DbSet<T> Set<T>() where T : class
        {
            if (!_sets.ContainsKey(typeof(T)))
            {
                EnsureTableCreated(typeof(T));
                _sets[typeof(T)] = new DbSet<T>(_provider);
            }
            return (DbSet<T>)_sets[typeof(T)];
        }
        private void EnsureTableCreated(Type entityType)
        {
            if (!_initializedEntities.Contains(entityType))
            {
                _provider.CreateTableFromEntity(entityType);
                _initializedEntities.Add(entityType);
            }
        }
        public void AddNewTables(IEnumerable<Type> entityTypes)
        {
            foreach (var entityType in entityTypes)
            {
                EnsureTableCreated(entityType);
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
