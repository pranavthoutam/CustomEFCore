namespace CustomEFCore.Migrations
{
    public class ScriptGenerator
    {
        public IEnumerable<string> GenerateScripts(IEnumerable<string> changes)
        {
            var scripts = new List<string>();

            foreach (var change in changes)
            {
                scripts.Add(change);
            }

            return scripts;
        }
    }
}
