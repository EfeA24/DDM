using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Messaging.KafkaOptions
{
    public class KafkaManageOptions
    {
        public const string SectionName = "Kafka";

        public string BootstrapServers { get; set; } = string.Empty;
        public string? SaslUsername { get; set; }
        public string? SaslPassword { get; set; }
        public string SecurityProtocol { get; set; } = Confluent.Kafka.SecurityProtocol.Plaintext.ToString();
        public string SaslMechanism { get; set; } = Confluent.Kafka.SaslMechanism.Plain.ToString();
        public string ClientId { get; set; } = "ddm-service";
    }
}
