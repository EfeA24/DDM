using Query.Application.Interfaces;
using Query.Application.Models;
using Query.Domain.Enums;
using Query.Domain.Models;
using Dapper;
using Query.Infrastructure.Sql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Query.Infrastructure.Providers
{
    public sealed class OracleReadAdapter : IReadProviderAdapter
    {
        private SqlBuilder _sqlBuilder;

        public OracleReadAdapter(SqlBuilder sqlBuilder)
            => _sqlBuilder = sqlBuilder ?? throw new ArgumentNullException(nameof(sqlBuilder));

        public DbEngine Engine => DbEngine.Oracle;

        public async Task<QueryResult> ExecuteAsync(string connectionString, ODataSpec spec, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrEmpty(connectionString);
            ArgumentNullException.ThrowIfNull(spec);

            await using var connection = new OracleConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            var selectStatement = _sqlBuilder.BuildSelect(spec, Dialect.Oracle);
            var command = new CommandDefinition(selectStatement.Sql, selectStatement.Parameters, cancellationToken: cancellationToken);
            var rows = await connection.QueryAsync(command).ConfigureAwait(false);
            var items = MapRows(rows);

            long? count = null;
            if (spec.Count)
            {
                var countStatement = _sqlBuilder.BuildCount(spec, Dialect.Oracle);
                var countCommand = new CommandDefinition(countStatement.Sql, countStatement.Parameters, cancellationToken: cancellationToken);
                count = await connection.ExecuteScalarAsync<long>(countCommand).ConfigureAwait(false);
            }

            return new QueryResult(items, count);
        }

        private static IReadOnlyList<DynamicRow> MapRows(IEnumerable<dynamic> rows)
        {
            var result = new List<DynamicRow>();
            foreach (var row in rows)
            {
                if (row is IDictionary<string, object> dictionary)
                {
                    var converted = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    foreach (var pair in dictionary)
                    {
                        converted[pair.Key] = pair.Value;
                    }

                    result.Add(DynamicRow.FromDictionary(converted));
                }
                else
                {
                    var converted = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    var runtimeType = row.GetType();
                    foreach (var property in runtimeType.GetProperties())
                    {
                        converted[property.Name] = property.GetValue(row);
                    }

                    result.Add(DynamicRow.FromDictionary(converted));
                }
            }

            return result;
        }
    }
}
