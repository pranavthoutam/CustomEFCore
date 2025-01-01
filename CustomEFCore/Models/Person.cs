using CustomEFCore.Attributes;

namespace CustomEFCore.Models
{
    public class Person
    {
        [PrimaryKey]
        public int Id { get; set; }
        public string? Name { get; set; }
        [FK("Address")]
        public int AddressId { get; set; }  

    }
    public class Address
    {
        public int Id { get; set; }
        public string Street { get; set; }

        //[FK("Product")]
        //public int ProductId { get; set; }
    }
}
