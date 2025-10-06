using Query.Application.Interfaces;
using Query.Application.Models;
using Query.Domain.Enums;
using Query.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Query.Application.Services
{
    public sealed class ExecutionPlanner
    {
        private readonly IConnectionRegistry _connectionRegistry;
        private readonly IReadOnlyDictionary<DbEngine, IReadProviderAdapter> _adapterByEngine;
        private readonly IRedisCache _redisCache;
        private readonly IMongoAggregate _mongoAggregate;

        public ExecutionPlanner(
            IConnectionRegistry connectionRegistry,
            IEnumerable<IReadProviderAdapter> adapters,
            IRedisCache redisCache,
            IMongoAggregate mongoAggregate)
        {
            _connectionRegistry = connectionRegistry ?? throw new ArgumentNullException(nameof(connectionRegistry));
            _redisCache = redisCache ?? throw new ArgumentNullException(nameof(redisCache));
            _mongoAggregate = mongoAggregate ?? throw new ArgumentNullException(nameof(mongoAggregate));

            if (adapters is null)
            {
                throw new ArgumentNullException(nameof(adapters));
            }

            _adapterByEngine = adapters.ToDictionary(adapter => adapter.Engine, adapter => adapter);
        }

        public async Task<QueryResult> ExecuteAsync(ODataSpec spec, CancellationToken cancellationToken)
        {
            if (spec is null)
            {
                throw new ArgumentNullException(nameof(spec));
            }

            if (!string.IsNullOrWhiteSpace(spec.Snapshot))
            {
                var snapshotResult = await _redisCache.TryGetSnapshotAsync(spec, cancellationToken).ConfigureAwait(false);
                if (snapshotResult is not null)
                {
                    return snapshotResult;
                }
            }

            var redisResult = await _redisCache.TryGetAsync(spec, cancellationToken).ConfigureAwait(false);
            if (redisResult is not null)
            {
                return redisResult;
            }

            var mongoResult = await _mongoAggregate.TryGetAsync(spec, cancellationToken).ConfigureAwait(false);
            if (mongoResult is not null)
            {
                return mongoResult;
            }

            var connection = _connectionRegistry.Get("Default");
            if (!_adapterByEngine.TryGetValue(connection.Engine, out var adapter))
            {
                throw new InvalidOperationException($"No read adapter registered for engine '{connection.Engine}'.");
            }

            return await adapter.ExecuteAsync(connection.ConnectionString, spec, cancellationToken).ConfigureAwait(false);
        }
    }
}
