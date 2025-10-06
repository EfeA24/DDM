using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Query.Domain.Models
{
    public sealed class QueryResult
    {
        public QueryResult(IReadOnlyList<DynamicRow> items, long? count)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
            Count = count;
        }

        public IReadOnlyList<DynamicRow> Items { get; }

        public long? Count { get; }

        public static QueryResult Empty { get; } = new(Array.Empty<DynamicRow>(), 0);
    }
}
