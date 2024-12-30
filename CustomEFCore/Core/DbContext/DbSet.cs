
namespace CustomEFCore.Core.DbContext
{
    public class DbSet<TEntity> where TEntity : class
    {
        private readonly CustomDbContext _context;
        private readonly List<TEntity> _entities = new List<TEntity>();

        public DbSet(CustomDbContext context)
        {
            _context = context;
        }

        // Add an entity to the DbSet
        public void Add(TEntity entity)
        {
            _entities.Add(entity);
            _context.SaveChanges();  // Placeholder save changes
        }

        // Remove an entity from the DbSet
        public void Remove(TEntity entity)
        {
            _entities.Remove(entity);
            _context.SaveChanges();  // Placeholder save changes
        }

        // Return IQueryable for queries
        public IQueryable<TEntity> AsQueryable()
        {
            return _entities.AsQueryable();
        }
    }
}
