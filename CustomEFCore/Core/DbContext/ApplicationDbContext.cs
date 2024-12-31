using CustomEFCore.Models;

namespace CustomEFCore.Core.DbContext
{
    public class AppDbContext : CustomDbContext
    {
        public AppDbContext(string connectionString) : base(connectionString) { }
        public DbSet<Product> Products { get; set; }
        public DbSet<Address> Address { get; set; }
        public DbSet<Person> Persons { get; set; }
    }
}
