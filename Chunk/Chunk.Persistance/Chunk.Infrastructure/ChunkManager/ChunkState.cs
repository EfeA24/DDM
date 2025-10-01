using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chunk.Infrastructure.ChunkManager
{
    public class ChunkSet
    {
        public Guid Id { get; set; }
        public string JobId { get; set; } = default!;
        public int Total { get; set; }
        public int Received { get; set; }
        public string Status { get; set; } = "Pending"; public List<string> Errors { get; set; } = new();
    }
    public class ChunkLog { public Guid Id { get; set; } public string JobId { get; set; } = default!; public int Index { get; set; } public string RedisKey { get; set; } = default!; public DateTime Ts { get; set; } }
    public class Failure { public Guid Id { get; set; } public string JobId { get; set; } = default!; public string Reason { get; set; } = default!; public DateTime Ts { get; set; } }
}
