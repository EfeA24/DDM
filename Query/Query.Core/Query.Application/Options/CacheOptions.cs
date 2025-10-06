using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Query.Application.Options
{
    public  class CacheOptions : IValidatableObject
    {
        public const string SectionName = "Cache";

        [Required]
        public RedisCacheOptions Redis { get; set; } = new();

        [Required]
        public MongoCacheOptions Mongo { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            yield break;
        }
    }
}
