namespace GranSteL.ScopesBalancer
{
    public class ScopeItemWrapper<T>
    {
        public ScopeItemWrapper(T scopeItem, ScopeContext context)
        {
            ScopeItem = scopeItem;
            Context = context;
        }

        /// <summary>
        /// Balanced item instance
        /// </summary>
        public T ScopeItem { get; }

        /// <summary>
        /// Invoking context
        /// </summary>
        public ScopeContext Context { get; }
    }
}