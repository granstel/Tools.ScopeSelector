namespace GranSteL.DialogflowBalancer
{
    public class DialogflowClientsConfiguration
    {
        public DialogflowClientsConfiguration(string projectId, string jsonPath)
        {
            ProjectId = projectId;
            JsonPath = jsonPath;
        }

        public string ProjectId { get; set; }

        public string JsonPath { get; set; }
    }
}
