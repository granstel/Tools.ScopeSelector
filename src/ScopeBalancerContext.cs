namespace GranSteL.ScopesBalancer
{
    public class ScopeBalancerContext
    {
        public ScopeBalancerContext(ScopeConfiguration configurations)
        {
            ScopeId = configurations.ScopeId;
            JsonPath = configurations.JsonPath;
        }

        public string ScopeId { get; }

        public string JsonPath { get; }
    }
}
