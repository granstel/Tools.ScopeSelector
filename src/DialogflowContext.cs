namespace GranSteL.DialogflowBalancer
{
    public class DialogflowContext
    {
        public DialogflowContext(DialogflowClientsConfiguration configurations)
        {
            ProjectId = configurations.ProjectId;
            JsonPath = configurations.JsonPath;
        }

        public string ProjectId { get; }

        public string JsonPath { get; }
    }
}
