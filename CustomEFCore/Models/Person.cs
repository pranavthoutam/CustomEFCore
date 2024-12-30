using CustomEFCore.Attributes;

namespace CustomEFCore.Models
{
    [DbModel]
    public class Person
    {
        [PrimaryKey]
        public int Id { get; set; }
        public string? Name { get; set; }
        [ForeignKey("Address")]
        public int AddressId { get; set; }  
    }
    [DbModel]
    public class Address
    {
        public int Id { get; set; }
        public string? Street { get; set; }
    }
}
