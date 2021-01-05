using System;
using System.Collections.Generic;

namespace GranSteL.DialogflowBalancer
{
    public class DialogflowBalancerConfiguration
    {
        public virtual TimeSpan ScopeExpiration { get; set; }

        public virtual ICollection<DialogflowClientsConfiguration> ClientsConfigurations { get; set; }
    }
}
