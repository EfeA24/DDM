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
    public class NoOpMongoAggregate : IMongoAggregate
    {
        public Task<QueryResult?> TryGetAsync(ODataSpec spec, CancellationToken cancellationToken)
            => Task.FromResult<QueryResult?>(null);
    }
}
