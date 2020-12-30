namespace GranSteL.DialogflowBalancer
{
    public class ClientWrapper<T>
    {
        public ClientWrapper(T client, string scopeKey)
        {
            Client = Client;
            ScopeKey = scopeKey;
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
    }
}