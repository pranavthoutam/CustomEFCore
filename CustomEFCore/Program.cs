using CustomEFCore.Core;
using CustomEFCore.Core.DbContext;
using CustomEFCore.Migrations;
using CustomEFCore.Models;

class Program
{
    static void Main(string[] args)
    {
        var connectionString = "Server=192.168.0.30;Database=EMP276;User Id=User5;Password=CDev005#8;Trusted_Connection=False;TrustServerCertificate=True;";
        var context = new CustomDbContext(connectionString);

        context.AddNewTables(new List<Type>
            {
                typeof(Product),
                typeof(Address),
                typeof(Person)
            });
        InsertDataIntoTables(context);

        //var schemaInfoProvider = new SchemaInfoProvider(connectionString);
        //var scriptGenerator = new ScriptGenerator();
        //var migrationManager = new MigrationManager(schemaInfoProvider, scriptGenerator);
        //var schemaUpdater = new SchemaUpdater(migrationManager);

        //schemaUpdater.UpdateSchema();


        context.Dispose();

        Console.WriteLine("Operations completed.");
    }
    static void InsertDataIntoTables(CustomDbContext context)
    {
        var addressSet = context.Set<Address>();
        var personSet = context.Set<Person>();
        var productSet = context.Set<Product>();
        

        var address = new Address { Id = 2, Street = "123 Main St" };
        addressSet.Add(address);

        var person = new Person { Id = 2, Name = "John Doe",AddressId = 2 };

        personSet.Add(person);

        var product = new Product { Id = 2, Name = "Laptop", Price = 1000.00m };
        productSet.Add(product);
    }
}
