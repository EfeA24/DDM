using Query.Application.Models;
using Query.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Query.Application.Interfaces
{
    public interface IMongoAggregate
    {
        Task<QueryResult?> TryGetAsync(ODataSpec spec, CancellationToken cancellationToken);
    }
}
