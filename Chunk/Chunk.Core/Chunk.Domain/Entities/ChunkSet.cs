using Chunk.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chunk.Domain.Entities
{
    public class ChunkSet
    {
        public string Id { get; set; } = default!;// JobId
        public string TenantId { get; set; } = default!;
        public int Total { get; set; }
        public int Received { get; set; }
        public ChunkSetStatus Status { get; set; } = ChunkSetStatus.Pending;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? CompletedAt { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
