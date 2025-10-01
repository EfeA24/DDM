using Chunk.Domain.Entities;
using Chunk.Infrastructure.Mongo;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Chunk.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChunkSetsController : ControllerBase
    {
        private readonly ChunkMongo _mongo;

        public ChunkSetsController(ChunkMongo mongo)
        {
            _mongo = mongo ?? throw new ArgumentNullException(nameof(mongo));
        }

        [HttpGet("{jobId}")]
        public async Task<ActionResult<ChunkSetResponse>> GetChunkSetAsync(string jobId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return BadRequest("Job id must be provided.");
            }

            var chunkSet = await _mongo.ChunkSets
                .Find(x => x.Id == jobId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (chunkSet is null)
            {
                return NotFound();
            }

            return Ok(ChunkSetResponse.From(chunkSet));
        }

        [HttpGet("{jobId}/chunks")]
        public async Task<ActionResult<IReadOnlyCollection<ChunkDocResponse>>> GetChunksAsync(string jobId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return BadRequest("Job id must be provided.");
            }

            var chunks = await _mongo.Chunks
                .Find(x => x.JobId == jobId)
                .SortBy(x => x.Index)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (chunks.Count == 0)
            {
                var exists = await _mongo.ChunkSets
                    .Find(x => x.Id == jobId)
                    .AnyAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (!exists)
                {
                    return NotFound();
                }
            }

            var response = chunks
                .Select(ChunkDocResponse.From)
                .ToList();

            return Ok(response);
        }

        [HttpGet("{jobId}/chunks/{index:int}")]
        public async Task<ActionResult<ChunkDocResponse>> GetChunkAsync(string jobId, int index, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(jobId))
            {
                return BadRequest("Job id must be provided.");
            }

            if (index < 0)
            {
                return BadRequest("Chunk index must be a non-negative integer.");
            }

            var chunk = await _mongo.Chunks
                .Find(x => x.JobId == jobId && x.Index == index)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (chunk is null)
            {
                return NotFound();
            }

            return Ok(ChunkDocResponse.From(chunk));
        }

        public sealed record ChunkSetResponse(
            string JobId,
            string TenantId,
            int Total,
            int Received,
            string Status,
            DateTimeOffset CreatedAt,
            DateTimeOffset? CompletedAt,
            IReadOnlyCollection<string> Errors)
        {
            public static ChunkSetResponse From(ChunkSet chunkSet)
            {
                ArgumentNullException.ThrowIfNull(chunkSet);

                var errors = chunkSet.Errors is { Count: > 0 }
                    ? chunkSet.Errors.ToArray()
                    : Array.Empty<string>();

                return new ChunkSetResponse(
                    chunkSet.Id,
                    chunkSet.TenantId,
                    chunkSet.Total,
                    chunkSet.Received,
                    chunkSet.Status.ToString(),
                    chunkSet.CreatedAt,
                    chunkSet.CompletedAt,
                    errors);
            }
        }

        public sealed record ChunkDocResponse(
            string JobId,
            string TenantId,
            int Index,
            long Size,
            string ContentHash,
            DateTimeOffset Ts)
        {
            public static ChunkDocResponse From(ChunkDoc chunk)
            {
                ArgumentNullException.ThrowIfNull(chunk);

                return new ChunkDocResponse(
                    chunk.JobId,
                    chunk.TenantId,
                    chunk.Index,
                    chunk.Size,
                    chunk.ContentHash,
                    chunk.Ts);
            }
        }
    }
}
