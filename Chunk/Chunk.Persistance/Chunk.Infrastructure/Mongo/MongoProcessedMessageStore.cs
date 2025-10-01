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
        private readonly IMongoCollection<ProcessedMessage> _collection;

        public MongoProcessedMessageStore(ChunkMongo mongo) => _collection = mongo.Processed;

        public async Task<bool> ExistsAsync(string messageId, CancellationToken ct)
        {
            var count = await _collection.CountDocumentsAsync(x => x.Id == messageId, cancellationToken: ct)
                                         .ConfigureAwait(false);
            return count > 0;
        }

        public async Task MarkProcessedAsync(string messageId, CancellationToken ct)
        {
            await _collection.InsertOneAsync(new ProcessedMessage { Id = messageId }, cancellationToken: ct)
                             .ConfigureAwait(false);
        }
    }
}
