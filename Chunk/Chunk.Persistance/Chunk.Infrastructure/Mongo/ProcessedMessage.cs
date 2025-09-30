using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chunk.Infrastructure.Mongo
{
    public sealed class ProcessedMessage
    {
        public string Id { get; set; } = default!;
        public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
