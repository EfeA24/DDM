using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chunk.Contracts
{
    public class ChunkPayloadReference
    {
        public sealed record ChunkPayloadReference(string RedisKey, DateTimeOffset? ExpiresAt = null);

    }
}
