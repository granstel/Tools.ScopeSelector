namespace GranSteL.ScopesBalancer
{
    public class ScopeConfiguration
    {
        public ScopeConfiguration(string scopeId, string jsonPath)
        {
            ScopeId = scopeId;
            JsonPath = jsonPath;
        }

        public string ScopeId { get; set; }

        public string JsonPath { get; set; }
    }
}
