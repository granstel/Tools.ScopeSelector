using System;
using System.Collections.Generic;

namespace GranSteL.Tools
{
    public class ScopesBalancerConfiguration
    {
        public virtual TimeSpan ScopeExpiration { get; set; }

        public virtual ICollection<DialogflowClientsConfiguration> ClientsConfigurations { get; set; }
    }
}
