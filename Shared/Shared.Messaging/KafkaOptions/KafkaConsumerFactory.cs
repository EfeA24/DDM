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
    public class KafkaConsumerFactory : IKafkaConsumerFactory
    {
        private readonly KafkaManageOptions _options;
        private readonly ILoggerFactory _loggerFactory;

        public KafkaConsumerFactory(IOptions<KafkaManageOptions> options, ILoggerFactory loggerFactory)
        {
            _options = options.Value;
            _loggerFactory = loggerFactory;
        }

        public IConsumer<string, string> Create(string groupId, string? clientId = null)
        {
            var config = BuildConsumerConfig(groupId, clientId);
            var logger = _loggerFactory.CreateLogger<KafkaConsumerFactory>();

            return new ConsumerBuilder<string, string>(config)
                .SetLogHandler((_, logMessage) => logger.LogDebug("Kafka consumer: {Message}", logMessage.Message))
                .SetErrorHandler((_, error) => logger.LogError("Kafka consumer error: {Error}", error))
                .Build();
        }

        private ConsumerConfig BuildConsumerConfig(string groupId, string? clientId)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _options.BootstrapServers,
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                EnablePartitionEof = true,
                ClientId = clientId ?? _options.ClientId
            };

            KafkaProducer.ApplySecurity(config, _options);
            return config;
        }
    }
}
