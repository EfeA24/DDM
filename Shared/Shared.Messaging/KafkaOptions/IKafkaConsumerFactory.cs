using Confluent.Kafka;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Messaging.KafkaOptions
{
    public interface IKafkaConsumerFactory
    {
        IConsumer<string, string> Create(string groupId, string? clientId = null);
    }
}
