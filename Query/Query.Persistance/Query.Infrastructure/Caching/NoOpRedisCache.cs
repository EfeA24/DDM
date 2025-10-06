using Query.Application.Interfaces;
using Query.Application.Models;
using Query.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Query.Infrastructure.Caching
{
    public sealed class NoOpRedisCache : IRedisCache
    {
        public Task<QueryResult?> TryGetSnapshotAsync(ODataSpec spec, CancellationToken cancellationToken)
            => Task.FromResult<QueryResult?>(null);

        public Task<QueryResult?> TryGetAsync(ODataSpec spec, CancellationToken cancellationToken)
            => Task.FromResult<QueryResult?>(null);
    }
}
