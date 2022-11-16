using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace GranSteL.Tools.ScopeSelector
{
    public class ScopesSelector<T>
    {
        /// <summary>
        /// Scope items
        /// </summary>
        private readonly ConcurrentBag<ScopeInstanceWrapper<T>> _items = new();

        public ScopesSelector(IEnumerable<ScopeContext> contexts, Func<ScopeContext, T> initInstance)
        {
            contexts = contexts.DistinctBy(c => c.ScopeId);

            foreach (var context in contexts)
            {
                if (!ScopeStorage.ScopesIds.Contains(context.ScopeId))
                {
                    ScopeStorage.ScopesIds.Enqueue(context.ScopeId);
                }

                var scopeInstance = initInstance(context);

                var item = new ScopeInstanceWrapper<T>(scopeInstance, context);

                _items.Add(item);
            }
        }

        public TResult Invoke<TResult>(Func<T, ScopeContext, TResult> invoke, string scopeId = null)
        {
            var item = GetItem(scopeId);

            var result = invoke(item.Instance, item.Context);

            return result;
        }

        private ScopeInstanceWrapper<T> GetItem(string scopeId = null)
        {
            scopeId ??= SelectScope();

            var scopeItem = _items.FirstOrDefault(item => string.Equals(item.Context.ScopeId, scopeId));

            return scopeItem;
        }

        private string SelectScope()
        {
            if (!ScopeStorage.ScopesIds.TryDequeue(out var scopeId))
            {
                return null;
            }

            ScopeStorage.ScopesIds.Enqueue(scopeId);

            return scopeId;
        }
    }
}
