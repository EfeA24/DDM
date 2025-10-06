using Query.Application.Models;
using Query.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Query.Application.Interfaces
{
    public interface IRedisCache
    {
        Task<QueryResult?> TryGetSnapshotAsync(ODataSpec spec, CancellationToken cancellationToken);

        Task<QueryResult?> TryGetAsync(ODataSpec spec, CancellationToken cancellationToken);
    }
}
