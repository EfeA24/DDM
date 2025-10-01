using Chunk.Application.Interfaces;
using Chunk.Infrastructure.Mongo;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Chunk.Application.Interfaces;
using Chunk.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Messaging;
using Shared.Messaging.KafkaOptions;
using StackExchange.Redis;
using Shared.Messaging.KafkaOptions;
using Shared.Messaging.MessagingOptions;

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
                    if (cr == null || cr.IsPartitionEOF) continue;

                    var env = MessagingSerializer.Deserialize<Envelope<IngestRawChunk>>(cr.Message.Value)!;
                    if (await _idempotency.ExistsAsync(env.Id, stoppingToken))
                    {
                        consumer.Commit(cr);
                        continue;
                    }

                    // 1) Redis'ten chunk payload'ını oku
                    var bytes = await _redis.StringGetAsync(env.Data.RedisKey);
                    if (bytes.IsNullOrEmpty)
                        throw new InvalidOperationException($"Redis key not found: {env.Data.RedisKey}");

                    // 2) Hash + normalize
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

                    // 3) Mongo state güncelle
                    await EnsureChunkSetAsync(env.Data, stoppingToken);
                    await UpsertChunkDocAsync(env.Data, hash, stoppingToken);

                    // 4) Publish normalized
                    var outEnv = new Envelope<NormalizedChunk>(
                        env.Id, "Ingest.NormalizedChunk.v1", "Chunk.Orchestrator",
                        env.CorrelationId, env.CausationId, env.TenantId,
                        DateTimeOffset.UtcNow, 1, normalized);

                    var outJson = MessagingSerializer.Serialize(outEnv);
                    await _producer.ProduceAsync(Topics.IngestNormalized, env.Id, outJson, null, stoppingToken);

                    // 5) İşlendi işaretle + offset commit
                    await _idempotency.MarkProcessedAsync(env.Id, stoppingToken);
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
                        await _producer.ProduceAsync(Topics.ErrorsDlq, cr.Message.Key ?? Guid.NewGuid().ToString(), dlq, null, stoppingToken);
                    }
                    _logger.LogError(ex, "ChunkOrchestrator failed");
                }
            }
        }
    }
