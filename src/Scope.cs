namespace GranSteL.Tools
{
    public class Scope
    {
        public Scope(string scopeName, int priority)
        {
            Name = scopeName;
            Priority = priority;
        }

        public string Name { get; set; }

        public int Priority { get; set; }
    }
}
