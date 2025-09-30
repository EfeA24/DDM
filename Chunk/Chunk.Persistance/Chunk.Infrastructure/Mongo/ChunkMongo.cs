using Chunk.Domain.Entities;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chunk.Infrastructure.Mongo
{
    public class ChunkMongo
    {
        public IMongoDatabase Db { get; }
        public IMongoCollection<ChunkSet> ChunkSets => Db.GetCollection<ChunkSet>("ChunkSets");
        public IMongoCollection<ChunkDoc> Chunks => Db.GetCollection<ChunkDoc>("Chunks");
        public IMongoCollection<ProcessedMessage> Processed => Db.GetCollection<ProcessedMessage>("ProcessedMessages");

        public ChunkMongo(IMongoClient client, string dbName) => Db = client.GetDatabase(dbName);
    }
}
