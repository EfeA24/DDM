using Chunk.Application.Interfaces;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chunk.Infrastructure.Mongo
{
    public sealed class MongoProcessedMessageStore : IProcessedMessageStore
    {
        private readonly IMongoCollection<ProcessedMessage> _col;
        public MongoProcessedMessageStore(ChunkMongo mongo) => _col = mongo.Processed;

        public async Task<bool> ExistsAsync(string messageId, CancellationToken ct)
        {
            var c = await _col.CountDocumentsAsync(x => x.Id == messageId, cancellationToken: ct);
            return c > 0;
        }

        public async Task MarkProcessedAsync(string messageId, CancellationToken ct)
        {
            await _col.InsertOneAsync(new ProcessedMessage { Id = messageId }, cancellationToken: ct);
        }
    }
}
