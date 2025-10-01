using System.Security.Cryptography;
using Chunk.Application.Interfaces;
using Chunk.Domain.Entities;
using Chunk.Domain.Enums;
using Chunk.Infrastructure.Mongo;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Shared.Messaging.KafkaOptions;
using Shared.Messaging.MessagingOptions;
using StackExchange.Redis;

namespace Chunk.Infrastructure.ChunkManager
{
    public sealed class ChunkOrchestrator : BackgroundService
    {
        private readonly IKafkaConsumerFactory _consumerFactory;
        private readonly IKafkaProducer _producer;
        private readonly ILogger<ChunkOrchestrator> _logger;
        private readonly ChunkMongo _mongo;
        private readonly IProcessedMessageStore _idempotency;
        private readonly IDatabase _redis;
        private readonly string _groupId;

        public ChunkOrchestrator(
            IKafkaConsumerFactory consumerFactory,
            IKafkaProducer producer,
            ILogger<ChunkOrchestrator> logger,
            ChunkMongo mongo,
            IProcessedMessageStore idempotency,
            IConnectionMultiplexer redis,
            string groupId = "chunk-orchestrator")
        {
            _consumerFactory = consumerFactory;
            _producer = producer;
            _logger = logger;
            _mongo = mongo;
            _idempotency = idempotency;
            _redis = redis.GetDatabase();
            _groupId = groupId;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var consumer = _consumerFactory.Create(_groupId);
            consumer.Subscribe(Topics.IngestRaw);
            _logger.LogInformation("ChunkOrchestrator subscribed {Topic}", Topics.IngestRaw);

            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? cr = null;
                try
                {
                    cr = consumer.Consume(stoppingToken);
                    if (cr == null || cr.IsPartitionEOF)
                    {
                        continue;
                    }

                    var env = MessagingSerializer.Deserialize<Envelope<IngestRawChunk>>(cr.Message.Value)!;
                    if (await _idempotency.ExistsAsync(env.Id, stoppingToken).ConfigureAwait(false))
                    {
                        consumer.Commit(cr);
                        continue;
                    }

                    var bytes = await _redis.StringGetAsync(env.Data.RedisKey).ConfigureAwait(false);
                    if (bytes.IsNullOrEmpty)
                    {
                        throw new InvalidOperationException($"Redis key not found: {env.Data.RedisKey}");
                    }

                    var payload = (byte[])bytes!;
                    var hash = Convert.ToHexString(SHA256.HashData(payload));

                    var normalized = new NormalizedChunk(
                        env.Id,
                        env.Data.TenantId,
                        env.Data.JobId,
                        env.Data.Index,
                        env.Data.Total,
                        env.Data.Size,
                        hash,
                        payload,
                        env.Data.SchemaVersion,
                        env.Data.Ts);

                    await EnsureChunkSetAsync(env.Data, stoppingToken).ConfigureAwait(false);
                    await UpsertChunkDocAsync(env.Data, hash, stoppingToken).ConfigureAwait(false);

                    var outEnv = new Envelope<NormalizedChunk>(
                        env.Id,
                        "Ingest.NormalizedChunk.v1",
                        "Chunk.Orchestrator",
                        env.CorrelationId,
                        env.CausationId,
                        env.TenantId,
                        DateTimeOffset.UtcNow,
                        1,
                        normalized);

                    var outJson = MessagingSerializer.Serialize(outEnv);
                    await _producer
                        .ProduceAsync(Topics.IngestNormalized, env.Id, outJson, null, stoppingToken)
                        .ConfigureAwait(false);

                    await _idempotency.MarkProcessedAsync(env.Id, stoppingToken).ConfigureAwait(false);
                    consumer.Commit(cr);
                }
                catch (Exception ex)
                {
                    if (cr != null)
                    {
                        var dlq = MessagingSerializer.Serialize(new
                        {
                            error = ex.Message,
                            topic = cr.Topic,
                            partition = cr.Partition.Value,
                            offset = cr.Offset.Value,
                            key = cr.Message.Key,
                            value = cr.Message.Value,
                            ts = DateTime.UtcNow
                        });

                        var dlqKey = cr.Message.Key ?? Guid.NewGuid().ToString();
                        await _producer
                            .ProduceAsync(Topics.ErrorsDlq, dlqKey, dlq, null, stoppingToken)
                            .ConfigureAwait(false);
                    }

                    _logger.LogError(ex, "ChunkOrchestrator failed");
                }
            }
        }

        private async Task EnsureChunkSetAsync(IngestRawChunk chunk, CancellationToken ct)
        {
            var filter = Builders<ChunkSet>.Filter.Eq(x => x.Id, chunk.JobId);
            var update = Builders<ChunkSet>.Update
                .SetOnInsert(x => x.Id, chunk.JobId)
                .SetOnInsert(x => x.TenantId, chunk.TenantId)
                .SetOnInsert(x => x.Total, chunk.Total)
                .SetOnInsert(x => x.Received, 0)
                .SetOnInsert(x => x.Status, ChunkSetStatus.Pending)
                .SetOnInsert(x => x.CreatedAt, DateTimeOffset.UtcNow);

            await _mongo.ChunkSets
                .UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, ct)
                .ConfigureAwait(false);
        }

        private async Task UpsertChunkDocAsync(IngestRawChunk chunk, string hash, CancellationToken ct)
        {
            var docId = $"{chunk.JobId}:{chunk.Index}";
            var filter = Builders<ChunkDoc>.Filter.Eq(x => x.Id, docId);
            var update = Builders<ChunkDoc>.Update
                .SetOnInsert(x => x.Id, docId)
                .Set(x => x.JobId, chunk.JobId)
                .Set(x => x.TenantId, chunk.TenantId)
                .Set(x => x.Index, chunk.Index)
                .Set(x => x.Size, chunk.Size)
                .Set(x => x.ContentHash, hash)
                .Set(x => x.Ts, chunk.Ts);

            var result = await _mongo.Chunks
                .UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, ct)
                .ConfigureAwait(false);

            if (result.UpsertedId == null)
            {
                return;
            }

            var setFilter = Builders<ChunkSet>.Filter.Eq(x => x.Id, chunk.JobId);
            var setUpdate = Builders<ChunkSet>.Update
                .Inc(x => x.Received, 1)
                .Set(x => x.Status, ChunkSetStatus.InProgress);

            var options = new FindOneAndUpdateOptions<ChunkSet>
            {
                ReturnDocument = ReturnDocument.After
            };

            var updatedSet = await _mongo.ChunkSets
                .FindOneAndUpdateAsync(setFilter, setUpdate, options, ct)
                .ConfigureAwait(false);

            if (updatedSet is null)
            {
                return;
            }

            if (updatedSet.Received >= updatedSet.Total)
            {
                await _mongo.ChunkSets.UpdateOneAsync(
                        setFilter,
                        Builders<ChunkSet>.Update
                            .Set(x => x.Status, ChunkSetStatus.Completed)
                            .Set(x => x.CompletedAt, DateTimeOffset.UtcNow),
                        cancellationToken: ct)
                    .ConfigureAwait(false);
            }
        }
    }

}

