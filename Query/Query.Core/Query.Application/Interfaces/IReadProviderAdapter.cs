using Query.Application.Models;
using Query.Domain.Enums;
using Query.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Query.Application.Interfaces
{
    public interface IReadProviderAdapter
    {
        DbEngine Engine { get; }

        Task<QueryResult> ExecuteAsync(string connectionString, ODataSpec spec, CancellationToken cancellationToken);
    }
}
