using Chunk.Contracts;
using Ingest.Application.Interfaces;
using Ingest.Domain.Entities;
using Microsoft.Extensions.Logging;
using Shared.Messaging.KafkaOptions;
using Shared.Messaging.MessagingOptions;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ingest.Application.Implementations
{
    public class RawChunkIngestService : IRawChunkIngestService
    {
        private static readonly TimeSpan DefaultPayloadTtl = TimeSpan.FromHours(1);

        private readonly IKafkaProducer _producer;
        private readonly IDatabase _redis;
        private readonly ILogger<RawChunkIngestService> _logger;

        public RawChunkIngestService(
            IKafkaProducer producer,
            IConnectionMultiplexer redis,
            ILogger<RawChunkIngestService> logger)
        {
            _producer = producer ?? throw new ArgumentNullException(nameof(producer));
            _redis = redis?.GetDatabase() ?? throw new ArgumentNullException(nameof(redis));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ChunkPayloadReference> IngestAsync(RawChunk chunk, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(chunk);

            var expiration = chunk.PayloadTtl ?? DefaultPayloadTtl;
            var redisKey = $"ingest:raw:{chunk.JobId}:{chunk.Index}:{chunk.MessageId}";

            var stored = await _redis.StringSetAsync(redisKey, chunk.Payload.ToArray(), expiration).ConfigureAwait(false);
            if (!stored)
            {
                throw new InvalidOperationException($"Failed to persist chunk payload in Redis for key '{redisKey}'.");
            }

            var envelope = new Envelope<IngestRawChunk>(
                chunk.MessageId,
                "Ingest.RawChunk.v1",
                nameof(RawChunkIngestService),
                chunk.CorrelationId,
                chunk.CausationId,
                chunk.TenantId,
                chunk.Timestamp,
                1,
                new IngestRawChunk(
                    chunk.MessageId,
                    chunk.TenantId,
                    chunk.JobId,
                    chunk.Index,
                    chunk.Total,
                    chunk.Size,
                    redisKey,
                    chunk.SchemaVersion,
                    chunk.Timestamp));

            var message = MessagingSerializer.Serialize(envelope);

            await _producer.ProduceAsync(Topics.IngestRaw, envelope.Id, message, headers: null, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "Ingested raw chunk {JobId}/{Index} ({Size} bytes) for tenant {TenantId}",
                chunk.JobId,
                chunk.Index,
                chunk.Size,
                chunk.TenantId);

            var expiresAt = DateTimeOffset.UtcNow.Add(expiration);
            return new ChunkPayloadReference(redisKey, expiresAt);
        }
    }
}
