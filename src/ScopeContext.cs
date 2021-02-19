namespace GranSteL.ScopesBalancer
{
    public class ScopeContext
    {
        public ScopeContext(string scopeId, string jsonPath)
        {
            ScopeId = scopeId;
            JsonPath = jsonPath;
        }

        public string ScopeId { get; set; }

        public string JsonPath { get; set; }
    }
}
