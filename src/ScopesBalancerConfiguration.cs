using System;
using System.Collections.Generic;

namespace GranSteL.ScopesBalancer
{
    public class ScopesBalancerConfiguration
    {
        public virtual TimeSpan ScopeExpiration { get; set; }

        public virtual ICollection<ScopeConfiguration> ClientsConfigurations { get; set; }
    }
}
