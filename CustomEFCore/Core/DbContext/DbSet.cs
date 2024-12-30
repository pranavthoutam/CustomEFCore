using CustomEFCore.Providers;

namespace CustomEFCore.Core.DbContext
{
    public class DbSet<T> where T : class
    {
        private readonly SqlServerProvider _provider;

        public DbSet(SqlServerProvider provider)
        {
            _provider = provider;
        }

        public void Add(T entity)
        {
            _provider.InsertEntity(entity);
        }

        public void Remove(T entity)
        {
            _provider.DeleteEntity(entity);
        }

        public List<T> ToList()
        {
            return _provider.GetEntities<T>();
        }

        public IQueryable<T> Where(Func<T, bool> predicate)
        {
            return ToList().AsQueryable().Where(predicate).AsQueryable();
        }


    }
}
