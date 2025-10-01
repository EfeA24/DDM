using Ingest.Infrastructure;
using Microsoft.Extensions.Options;
using Shared.Messaging;
using Shared.Contracts;
using System.Text.Json;
using Shared.Messaging.KafkaOptions;
using Ingest.Infrastructure.RedisOptions;
using Shared.Contracts.Ingest;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IKafkaProducer, KafkaProducer>();
builder.Services.AddSingleton(StackExchange.Redis.ConnectionMultiplexer.Connect(
    builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379"));
builder.Services.AddSingleton<RedisBatchReader>();
builder.Services.Configure<IngestOptions>(builder.Configuration.GetSection("Ingest"));
builder.Services.AddHostedService<IngestPublisherWorker>();
var app = builder.Build();
app.MapGet("/", () => "Ingest OK");
app.Run();

record IngestOptions { public string RedisListKey { get; init; } = "ingest:queue"; public int BatchSize { get; init; } = 100; public string Topic { get; init; } = "ingest.raw"; }

sealed class IngestPublisherWorker(ILogger<IngestPublisherWorker> log, RedisBatchReader redis, IKafkaProducer producer, IOptions<IngestOptions> opt) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var items = await redis.TakeBatchAsync(opt.Value.RedisListKey, opt.Value.BatchSize, ct);
            foreach (var item in items)
            {
                var msg = JsonSerializer.Deserialize<IngestRawMessage>(item)!;

                await producer.ProduceAsync(opt.Value.Topic, msg.MessageId, JsonSerializer.Serialize(msg), null, ct);
            }
            await Task.Delay(200, ct);
        }
    }
}
