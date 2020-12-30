namespace GranSteL.DialogflowBalancer
{
    public class ClientWrapper<T>
    {
        public ClientWrapper(T client)
        {
            Client = Client;
        }
        
        /// <summary>
        /// Number of uses
        /// </summary>
        public int Load { get; set; }

        /// <summary>
        /// Client instance
        /// </summary>
        public T Client { get; }
    }
}