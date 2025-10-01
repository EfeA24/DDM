using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Messaging.KafkaOptions
{
    internal sealed class KafkaProducer : IKafkaProducer
    {
        private readonly IProducer<string, string> _producer;
        private readonly ILogger<KafkaProducer> _logger;

        public KafkaProducer(IOptions<KafkaOptions> options, ILogger<KafkaProducer> logger)
        {
            _logger = logger;
            var config = BuildProducerConfig(options.Value);
            _producer = new ProducerBuilder<string, string>(config)
                .SetLogHandler((_, log) => _logger.LogDebug("Kafka producer: {Message}", log.Message))
                .SetErrorHandler((_, error) => _logger.LogError("Kafka producer error: {Error}", error))
                .Build();
        }

        public async Task ProduceAsync(string topic, string key, string value, Headers? headers, CancellationToken cancellationToken)
        {
            var message = new Message<string, string>
            {
                Key = key,
                Value = value,
                Headers = headers ?? new Headers()
            };

            var result = await _producer.ProduceAsync(topic, message, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Kafka message produced to {Topic} partition {Partition} offset {Offset}", result.Topic, result.Partition, result.Offset);
        }

        public ValueTask DisposeAsync()
        {
            _producer.Flush();
            _producer.Dispose();
            return ValueTask.CompletedTask;
        }

        private static ProducerConfig BuildProducerConfig(KafkaOptions options)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = options.BootstrapServers,
                ClientId = options.ClientId,
                EnableIdempotence = true,
                Acks = Acks.All,
                MessageSendMaxRetries = 3,
                LingerMs = 5
            };

            ApplySecurity(config, options);
            return config;
        }

        internal static void ApplySecurity(ClientConfig config, KafkaOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.SaslUsername) || string.IsNullOrWhiteSpace(options.SaslPassword))
            {
                return;
            }

            if (Enum.TryParse<SecurityProtocol>(options.SecurityProtocol, true, out var protocol))
            {
                config.SecurityProtocol = protocol;
            }

            if (Enum.TryParse<SaslMechanism>(options.SaslMechanism, true, out var mechanism))
            {
                config.SaslMechanism = mechanism;
            }

            config.SaslUsername = options.SaslUsername;
            config.SaslPassword = options.SaslPassword;
        }
    }
}
