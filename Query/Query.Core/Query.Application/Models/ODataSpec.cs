using Microsoft.Extensions.Primitives;
using Query.Application.Filtering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Query.Application.Models
{
    public sealed class ODataSpec
    {
        private ODataSpec(
            string source,
            IReadOnlyList<string> select,
            IReadOnlyList<OrderClause> orderBy,
            FilterNode? filter,
            int? top,
            int? skip,
            bool count,
            string? snapshot)
        {
            Source = source;
            Select = select;
            OrderBy = orderBy;
            Filter = filter;
            Top = top;
            Skip = skip;
            Count = count;
            Snapshot = snapshot;
        }

        public string Source { get; }

        public IReadOnlyList<string> Select { get; }

        public IReadOnlyList<OrderClause> OrderBy { get; }

        public FilterNode? Filter { get; }

        public int? Top { get; }

        public int? Skip { get; }

        public bool Count { get; }

        public string? Snapshot { get; }

        public static ODataSpec From(IDictionary<string, StringValues> query)
        {
            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var source = GetFirstValue(query, "source");
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentException("The source parameter is required.", nameof(query));
            }

            var select = ParseSelect(GetFirstValue(query, "$select"));
            var orderBy = ParseOrderBy(GetFirstValue(query, "$orderby"));
            var filter = ODataFilterParser.Parse(GetFirstValue(query, "$filter"));
            var top = ParseNullableInt(GetFirstValue(query, "$top"));
            var skip = ParseNullableInt(GetFirstValue(query, "$skip"));
            var count = IsTrue(GetFirstValue(query, "$count"));
            var snapshot = GetFirstValue(query, "snapshot");

            return new ODataSpec(source, select, orderBy, filter, top, skip, count, snapshot);
        }

        private static IReadOnlyList<string> ParseSelect(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Array.Empty<string>();
            }

            return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();
        }

        private static IReadOnlyList<OrderClause> ParseOrderBy(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Array.Empty<OrderClause>();
            }

            var clauses = new List<OrderClause>();
            var segments = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var segment in segments)
            {
                var trimmed = segment.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }

                var descending = trimmed.EndsWith(" desc", StringComparison.OrdinalIgnoreCase);
                var ascending = trimmed.EndsWith(" asc", StringComparison.OrdinalIgnoreCase);

                string field = trimmed;
                if (descending)
                {
                    field = trimmed[..^5].Trim();
                }
                else if (ascending)
                {
                    field = trimmed[..^4].Trim();
                }

                if (string.IsNullOrWhiteSpace(field))
                {
                    continue;
                }

                clauses.Add(new OrderClause(field, descending));
            }

            return clauses;
        }

        private static int? ParseNullableInt(string? value)
        {
            if (int.TryParse(value, out var result))
            {
                return result;
            }

            return null;
        }

        private static bool IsTrue(string? value)
            => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

        private static string? GetFirstValue(IDictionary<string, StringValues> query, string key)
        {
            if (!query.TryGetValue(key, out var values))
            {
                return null;
            }

            return values.Count > 0 ? values[0] : null;
        }
    }
}
