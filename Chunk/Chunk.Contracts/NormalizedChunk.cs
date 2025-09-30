using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chunk.Contracts
{
    public record NormalizedChunk(
        string MessageId,
        string TenantId,
        string JobId,
        int Index,
        int Total,
        long Size,
        string ContentHash,
        byte[] Data,
        int SchemaVersion,
        DateTimeOffset Ts);
}
