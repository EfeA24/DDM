using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Messaging.KafkaOptions
{
    public interface IKafkaProducer : IAsyncDisposable
    {
        Task ProduceAsync(string topic, string key, string value, Headers? headers, CancellationToken cancellationToken);
    }
}
