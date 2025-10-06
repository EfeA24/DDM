using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sink.Application.Routing.Interfaces
{
    public interface IConnectionStringRegistry
    {
        Task<string?> GetAsync(string key, CancellationToken cancellationToken);
    }
}
