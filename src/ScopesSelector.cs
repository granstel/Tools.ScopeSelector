using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GranSteL.Tools.ScopeSelector.Extensions;

namespace GranSteL.Tools.ScopeSelector
{
    public class ScopesSelector<T>
    {
        private readonly IScopeBindingStorage _bindingStorage;
        private readonly int _parallelScopes;

        private readonly ConcurrentBag<ScopeItemWrapper<T>> _scopeItems;

        public ScopesSelector(
            IScopeBindingStorage bindingStorage,
            ICollection<ScopeContext> scopesContexts,
            Func<ScopeContext, T> initScopeItem,
            int parallelScopes = 1
            )
        {
            _bindingStorage = bindingStorage;
            _parallelScopes = parallelScopes;

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

        public TResult Invoke<TResult>(string bindingKey, Func<IEnumerable<ScopeItemWrapper<T>>, TResult> invoke, params string[] suggestedScopeId)
        {
            var scopeWrapper = GetScopeItem(bindingKey, suggestedScopeId);

            var result = invoke(scopeWrapper);

            return result;
        }

        private IEnumerable<ScopeItemWrapper<T>> GetScopeItem(string bindingKey, params string[] suggestedScopeId)
        {
            var scopeItems = _scopeItems.Where(s => suggestedScopeId.Contains(s.Context.ScopeId)).ToList();

            if (!scopeItems.Any())
            {
                if (!_bindingStorage.TryGet(bindingKey, out ICollection<string> scopeId))
                {
                    for (var i = 0; i < _parallelScopes; i++)
                    {
                        scopeId.Add(SelectScope());
                        
                    }
                    
                    scopeItems.Add(_scopeItems.FirstOrDefault(s => scopeId.Contains(s.Context.ScopeId)));
                }

                _bindingStorage.Add(bindingKey, scopeItems.Select(i => i.Context.ScopeId).ToArray());
            }

            return scopeItems;
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
