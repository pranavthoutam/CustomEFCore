
using CustomEFCore.Attributes;

namespace CustomEFCore.Models
{
    public class Order
    {
        [PrimaryKey]
        public string Name { get; set; }

        public float Price { get; set; }
    }
}
