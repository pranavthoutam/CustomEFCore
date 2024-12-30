using CustomEFCore.Core.DbContext;
using CustomEFCore.Models;
using CustomEFCore.Providers;
using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        // Connection string for the "master" database
        var connectionString = "Server=LAPTOP-B464VUCR;Database=master;Integrated Security=SSPI;TrustServerCertificate=True;";

        // Create the SqlServerProvider to ensure the database exists
        var sqlProvider = new SqlServerProvider(connectionString);

        // Ensure the target database exists and is created if necessary
        sqlProvider.EnsureDatabaseCreated();

        // Now switch to the target database (CustomEfCore)
        sqlProvider.SwitchToTargetDatabase();

        // Create a new CustomDbContext instance for the target database
        var context = new CustomDbContext(connectionString);

        // Get the entity types (in your case, you have a Person class)
        var entityTypes = new List<Type> { typeof(Person) };

        // Create tables for the entity types
        sqlProvider.CreateTablesFromModel(entityTypes);

        // Add a person entity to the database
        var personSet = context.Set<Person>();
        personSet.Add(new Person { Id = 1, Name = "Alice" });
        personSet.Add(new Person { Id = 2, Name = "Bob" });

        // Save changes to the database
        context.SaveChanges();

        // Dispose the context after operations
        context.Dispose();

        Console.WriteLine("Database and table operations completed.");
    }
}
