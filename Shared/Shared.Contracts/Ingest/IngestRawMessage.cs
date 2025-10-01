using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts.Ingest
{
    public record IngestRawMessage(
        string MessageId,
        string TenantId,
        SourceInfo Source,
        ChunkInfo Chunk,
        PayloadRef PayloadRef,
        int SchemaVersion,
        DateTime Ts);
}
