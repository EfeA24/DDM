using Chunk.Contracts;
using Ingest.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ingest.Application.Interfaces
{
    public interface IRawChunkIngestService
    {
        Task<ChunkPayloadReference> IngestAsync(RawChunk chunk, CancellationToken cancellationToken = default);
    }
}
