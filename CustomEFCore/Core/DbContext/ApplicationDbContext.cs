using CustomEFCore.Models;

namespace CustomEFCore.Core.DbContext
{
    public class ApplicationDbContext : CustomDbContext
    {
        public ApplicationDbContext(string connectionString) : base(connectionString) { }
        public DbSet<Product> Products { get; set; }
        public DbSet<Address> Address { get; set; }
        public DbSet<Person> Persons { get; set; }
        
        public DbSet<Order> Orders { get; set; }
    }
}
