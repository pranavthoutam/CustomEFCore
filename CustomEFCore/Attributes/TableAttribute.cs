namespace CustomEFCore.Attributes
{
    [AttributeUsage(AttributeTargets.Class,AllowMultiple =false)]
    public class TableAttribute : Attribute
    {
        public string Name { get; }
        public TableAttribute(string name) => Name = name;
    }
}
