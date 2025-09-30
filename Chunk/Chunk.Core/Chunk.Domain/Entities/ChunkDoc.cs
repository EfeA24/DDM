using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chunk.Domain.Entities
{
    public class ChunkDoc
    {
        public string Id { get; set; } = default!; // $"{JobId}:{Index}"
        public string JobId { get; set; } = default!;
        public string TenantId { get; set; } = default!;
        public int Index { get; set; }
        public long Size { get; set; }
        public string ContentHash { get; set; } = default!;
        public DateTimeOffset Ts { get; set; }
    }
}
