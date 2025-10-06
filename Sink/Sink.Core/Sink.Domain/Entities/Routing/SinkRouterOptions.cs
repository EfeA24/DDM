using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sink.Domain.Entities.Routing
{
    public class SinkRouterOptions
    {
        public string? DefaultConnectionKey { get; set; }
        public bool AllowEphemeralConnectionString { get; set; }
        public IList<string> AllowedHosts { get; set; } = new List<string>();
        public IList<string> DenylistKeywords { get; set; } = new List<string>();
    }
}
