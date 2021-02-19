namespace GranSteL.Tools
{
    public class ScopeBalancerContext
    {
        public ScopeBalancerContext(DialogflowClientsConfiguration configurations)
        {
            ProjectId = configurations.ProjectId;
            JsonPath = configurations.JsonPath;
        }

        public string ProjectId { get; }

        public string JsonPath { get; }
    }
}
