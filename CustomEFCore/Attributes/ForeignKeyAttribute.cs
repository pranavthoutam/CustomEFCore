namespace CustomEFCore.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class FKAttribute : Attribute
    {
        public string ReferencedTable { get; }

        public FKAttribute(string referencedTable)
        {
            ReferencedTable = referencedTable;
        }
    }
}
