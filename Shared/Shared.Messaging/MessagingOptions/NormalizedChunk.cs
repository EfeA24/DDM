using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Messaging.MessagingOptions
{
    public sealed record NormalizedChunk(
        string MessageId,
        string TenantId,
        string JobId,
        int Index,
        int Total,
        long Size,
        string ContentHash,
        byte[] Payload,
        int SchemaVersion,
        DateTimeOffset Ts);
}
