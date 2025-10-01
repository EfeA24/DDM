using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Messaging.KafkaOptions
{
    public sealed record Envelope<T>(
        string Id,
        string Type,
        string Source,
        string? CorrelationId,
        string? CausationId,
        string? TenantId,
        DateTimeOffset CreatedAt,
        int Version,
        T Data);
}
