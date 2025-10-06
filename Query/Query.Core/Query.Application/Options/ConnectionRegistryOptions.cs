using Query.Application.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Query.Application.Options
{
    public sealed class ConnectionRegistryOptions : IValidatableObject
    {
        public const string SectionName = "Connections";

        [Required]
        public Dictionary<string, ConnectionEntry> Entries { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Entries.Count == 0)
            {
                yield return new ValidationResult("At least one connection entry must be configured.", new[] { nameof(Entries) });
            }

            foreach (var (key, entry) in Entries)
            {
                if (entry is null)
                {
                    yield return new ValidationResult($"Connection entry '{key}' is null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.ConnectionString))
                {
                    yield return new ValidationResult($"Connection '{key}' must provide a connection string.");
                }
            }
        }
    }
}
