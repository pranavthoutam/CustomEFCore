using CustomEFCore.Attributes;

namespace CustomEFCore.Entities
{
    [Table("Users")]
    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
