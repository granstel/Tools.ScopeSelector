namespace GranSteL.ScopesBalancer
{
    public class Scope
    {
        public Scope(string scopeId, int priority)
        {
            Id = scopeId;
            Priority = priority;
        }

        public string Id { get; set; }

        public int Priority { get; set; }
    }
}
