namespace GranSteL.DialogflowBalancer
{
    public class DialogflowClientWrapper<T>
    {
        public DialogflowClientWrapper(T client, DialogflowContext context)
        {
            Client = Client;
            ScopeKey = context.ProjectId;
            Context = context;
        }

        /// <summary>
        /// Number of uses
        /// </summary>
        public int Load { get; set; }

        /// <summary>
        /// Scope key (for example, Df agent id)
        /// </summary>
        public string ScopeKey { get; set; }

        /// <summary>
        /// Client instance
        /// </summary>
        public T Client { get; }

        /// <summary>
        /// Client context
        /// </summary>
        public DialogflowContext Context { get; }
    }
}