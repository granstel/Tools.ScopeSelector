namespace GranSteL.ScopesBalancer
{
    public class ScopeWrapper<T>
    {
        public ScopeWrapper(T balancedScopeItem, ScopeContext context)
        {
            BalancedScopeItem = balancedScopeItem;
            ScopeId = context.ScopeId;
            Context = context;
        }

        /// <summary>
        /// Scope key (for example, Df agent id)
        /// </summary>
        public string ScopeId { get; set; }

        /// <summary>
        /// Balanced item instance
        /// </summary>
        public T BalancedScopeItem { get; }

        /// <summary>
        /// Invoking context
        /// </summary>
        public ScopeContext Context { get; }
    }
}