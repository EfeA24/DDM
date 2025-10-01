using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ingest.Infrastructure.RedisOptions
{
    public class RedisBatchReader(IConnectionMultiplexer mux)
    {
        public async Task<IReadOnlyList<string>> TakeBatchAsync(string listKey, int batchSize, CancellationToken ct)
        {
            var db = mux.GetDatabase();
            var tran = db.CreateTransaction();
            var tasks = new List<Task<RedisValue>>();
            for (int i = 0; i < batchSize; i++) tasks.Add(tran.ListLeftPopAsync(listKey));
            await tran.ExecuteAsync();
            var values = await Task.WhenAll(tasks);
            return values.Where(v => !v.IsNullOrEmpty).Select(v => (string)v!).ToList();
        }
    }
}
