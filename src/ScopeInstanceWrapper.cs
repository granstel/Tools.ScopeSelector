namespace GranSteL.Tools.ScopeSelector
{
    /// <summary>
    /// Scope Instance wrapper (shortly - scope item), that contains instance and context at selected scope
    /// </summary>
    /// <typeparam name="T">Instance type</typeparam>
    internal class ScopeInstanceWrapper<T>
    {
        /// <summary>
        /// Initialization
        /// </summary>
        /// <param name="instance">Instance, that will be used at scope</param>
        /// <param name="context">Context of a scope</param>
        public ScopeInstanceWrapper(T instance, ScopeContext context)
        {
            Instance = instance;
            Context = context;
        }

        /// <summary>
        /// Instance, that will be used at scope
        /// </summary>
        public T Instance { get; }

        /// <summary>
        /// Context of a scope
        /// </summary>
        public ScopeContext Context { get; }
    }
}