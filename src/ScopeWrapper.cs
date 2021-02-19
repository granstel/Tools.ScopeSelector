namespace GranSteL.Tools
{
    public class ScopeWrapper<T>
    {
        public ScopeWrapper(T balancedScopeItem, ScopeBalancerContext context)
        {
            BalancedScopeItem = balancedScopeItem;
            ScopeKey = context.ProjectId;
            Context = context;
        }

        /// <summary>
        /// Scope key (for example, Df agent id)
        /// </summary>
        public string ScopeKey { get; set; }

        /// <summary>
        /// Balanced item instance
        /// </summary>
        public T BalancedScopeItem { get; }

        /// <summary>
        /// Client context
        /// </summary>
        public ScopeBalancerContext Context { get; }
    }
}