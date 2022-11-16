using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace GranSteL.Tools.ScopeSelector
{
    public class ScopesSelector<T>
    {
        private readonly IScopeBindingStorage _bindingStorage;

        private readonly ConcurrentBag<ScopeItemWrapper<T>> _scopeItems;

        public ScopesSelector(
            IScopeBindingStorage bindingStorage,
            IEnumerable<ScopeContext> scopesContexts,
            Func<ScopeContext, T> initScopeItem
            )
        {
            _bindingStorage = bindingStorage;

            _scopeItems = new ConcurrentBag<ScopeItemWrapper<T>>();

            var contexts = scopesContexts.DistinctBy(c => c.ScopeId).ToList();

            foreach (var context in contexts)
            {
                if (!ScopeStorage.ScopesIds.Contains(context.ScopeId))
                {
                    ScopeStorage.ScopesIds.Enqueue(context.ScopeId);
                }

                var scopeItem = initScopeItem(context);

                var wrapper = new ScopeItemWrapper<T>(scopeItem, context);

                _scopeItems.Add(wrapper);
            }
        }

        public TResult Invoke<TResult>(string bindingKey, Func<T, ScopeContext, TResult> invoke, string suggestedScopeId = null)
        {
            var scopeWrapper = GetScopeItem(bindingKey, suggestedScopeId);

            var result = invoke(scopeWrapper.ScopeItem, scopeWrapper.Context);

            return result;
        }

        private ScopeItemWrapper<T> GetScopeItem(string bindingKey, string suggestedScopeId = null)
        {
            var scopeItem = _scopeItems.FirstOrDefault(s => string.Equals(s.Context.ScopeId, suggestedScopeId));

            if (scopeItem == null)
            {
                if (!_bindingStorage.TryGet(bindingKey, out string scopeId))
                {
                    scopeId = SelectScope();
                }

                scopeItem = _scopeItems.FirstOrDefault(s => string.Equals(s.Context.ScopeId, scopeId));
            }

            if (scopeItem == null)
            {
                var scopeId = SelectScope();

                scopeItem = _scopeItems.FirstOrDefault(s => string.Equals(s.Context.ScopeId, scopeId));
            }

            _bindingStorage.Add(bindingKey, scopeItem.Context.ScopeId);

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
