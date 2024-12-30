namespace CustomEFCore.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ForeignKeyAttribute : Attribute
    {
        public string ReferencedTable { get; }

        public ForeignKeyAttribute(string referencedTable)
        {
            ReferencedTable = referencedTable;
        }
    }
}
