using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Query.Domain.Models
{
    public sealed class DynamicRow
    {
        private readonly IReadOnlyDictionary<string, object?> _values;

        public DynamicRow(string id, IReadOnlyDictionary<string, object?> values)
        {
            __id = id ?? throw new ArgumentNullException(nameof(id));
            _values = values ?? throw new ArgumentNullException(nameof(values));
            __dyn = new ReadOnlyDictionary<string, object?>(values as IDictionary<string, object?>
                ?? new Dictionary<string, object?>(values));
        }

        [JsonPropertyName("__id")]
        public string __id { get; }

        [JsonPropertyName("__dyn")]
        public IReadOnlyDictionary<string, object?> __dyn { get; }

        public static DynamicRow FromDictionary(IDictionary<string, object?> values, string? id = null)
        {
            if (values is null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            var sanitized = new Dictionary<string, object?>(values.Count, StringComparer.OrdinalIgnoreCase);
            foreach (var pair in values)
            {
                if (pair.Key is null)
                {
                    continue;
                }

                sanitized[pair.Key] = pair.Value;
            }

            var resolvedId = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id!;
            return new DynamicRow(resolvedId, sanitized);
        }
    }
}
