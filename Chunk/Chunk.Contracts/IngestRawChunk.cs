using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chunk.Contracts
{
    public record IngestRawChunk(
        string MessageId,
        string TenantId,
        string JobId,
        int Index,
        int Total,
        long Size,
        string RedisKey,       // payloadRef.redisKey
        int SchemaVersion,
        DateTimeOffset Ts);
}
