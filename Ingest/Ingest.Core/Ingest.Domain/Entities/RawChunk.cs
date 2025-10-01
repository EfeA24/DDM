using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ingest.Domain.Entities
{
    public sealed class RawChunk
    {
        private readonly byte[] _payload;

        public RawChunk(
            string tenantId,
            string jobId,
            int index,
            int total,
            ReadOnlyMemory<byte> payload,
            int schemaVersion,
            string? messageId = null,
            string? correlationId = null,
            string? causationId = null,
            DateTimeOffset? timestamp = null,
            TimeSpan? payloadTtl = null)
        {
            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentException("TenantId is required", nameof(tenantId));
            }

            if (string.IsNullOrWhiteSpace(jobId))
            {
                throw new ArgumentException("JobId is required", nameof(jobId));
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Chunk index cannot be negative.");
            }

            if (total <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(total), total, "Total chunk count must be positive.");
            }

            if (index >= total)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "Chunk index cannot exceed the total number of chunks.");
            }

            if (payload.IsEmpty)
            {
                throw new ArgumentException("Chunk payload cannot be empty", nameof(payload));
            }

            if (schemaVersion <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(schemaVersion), schemaVersion, "Schema version must be positive.");
            }

            if (payloadTtl is { Ticks: < 0 })
            {
                throw new ArgumentOutOfRangeException(nameof(payloadTtl), payloadTtl, "Payload TTL cannot be negative.");
            }

            TenantId = tenantId;
            JobId = jobId;
            Index = index;
            Total = total;
            _payload = payload.ToArray();
            SchemaVersion = schemaVersion;
            MessageId = string.IsNullOrWhiteSpace(messageId) ? Guid.NewGuid().ToString("N") : messageId!;
            CorrelationId = correlationId;
            CausationId = causationId;
            Timestamp = timestamp ?? DateTimeOffset.UtcNow;
            PayloadTtl = payloadTtl;
        }

        public string MessageId { get; }

        public string TenantId { get; }

        public string JobId { get; }

        public int Index { get; }

        public int Total { get; }

        public int SchemaVersion { get; }

        public string? CorrelationId { get; }

        public string? CausationId { get; }

        public DateTimeOffset Timestamp { get; }

        public TimeSpan? PayloadTtl { get; }

        public long Size => _payload.LongLength;

        public ReadOnlyMemory<byte> Payload => _payload;
    }
}
