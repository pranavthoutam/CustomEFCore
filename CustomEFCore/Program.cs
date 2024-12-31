using CustomEFCore.Core.DbContext;
using CustomEFCore.Models;

class Program
{
    static void Main(string[] args)
    {
        var connectionString = "Server=192.168.0.30;Database=EMP276;User Id=User5;Password=CDev005#8;Trusted_Connection=False;TrustServerCertificate=True;";

        using (var context = new AppDbContext(connectionString))
        {
            //context.Products.Add(new Product { Id = 3, Price = 999.99m });
            //context.Address.Add(new Address { Id = 1, Street = "123 Main St" });
            //context.Persons.Add(new Person { Id = 2, Name = "John Doe", AddressId = 1,MobileNumber="Hello" });

            //Product removePerson = context.Products.ToList().FirstOrDefault(p=>p.Id==2);
            //Address address = context.Addresses.ToList().FirstOrDefault(a=>a.Id == 1);
            //context.Addresses.Remove(address);

            context.Persons.Update(new Person { Id = 1,Name="Pranav" });
            context.SaveChanges();
        }



        //var schemaInfoProvider = new SchemaInfoProvider(connectionString);
        //var scriptGenerator = new ScriptGenerator();
        //var migrationManager = new MigrationManager(schemaInfoProvider, scriptGenerator);
        //var schemaUpdater = new SchemaUpdater(migrationManager);

        //schemaUpdater.UpdateSchema();




        Console.WriteLine("Operations completed.");
    }
    
}
