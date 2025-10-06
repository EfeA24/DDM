using Microsoft.Extensions.Options;
using Query.Application.Interfaces;
using Query.Application.Models;
using Query.Application.Options;
using Query.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Query.Infrastructure.Configuration
{
    public class ConnectionRegistry : IConnectionRegistry
    {
        private readonly IOptionsMonitor<ConnectionRegistryOptions> _optionsMonitor;

        public ConnectionRegistry(IOptionsMonitor<ConnectionRegistryOptions> optionsMonitor)
            => _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));

        public ConnectionInfo Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Connection name cannot be empty.", nameof(name));
            }

            var options = _optionsMonitor.CurrentValue ?? throw new InvalidOperationException("Connection registry options are not configured.");
            if (!options.Entries.TryGetValue(name, out var entry) || entry is null)
            {
                throw new KeyNotFoundException($"Connection named '{name}' was not found.");
            }

            if (string.IsNullOrWhiteSpace(entry.ConnectionString))
            {
                throw new InvalidOperationException($"Connection '{name}' does not define a connection string.");
            }

            var engine = entry.Engine is null or DbEngine.Unknown
                ? DetectEngine(entry.ConnectionString)
                : entry.Engine.Value;

            return new ConnectionInfo(name, entry.ConnectionString, engine);
        }

        private static DbEngine DetectEngine(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string cannot be empty.", nameof(connectionString));
            }

            if (HasAny(connectionString, "Host=", "Username=", "SearchPath=", "Port="))
            {
                return DbEngine.PostgreSql;
            }

            if (HasAny(connectionString, "Server=", "Data Source=", "Initial Catalog=", "Trusted_Connection"))
            {
                return DbEngine.SqlServer;
            }

            if (Contains(connectionString, "User Id=") &&
                HasAny(connectionString, "Data Source=", "SID=", "SERVICE NAME=", "SERVICE_NAME="))
            {
                return DbEngine.Oracle;
            }

            throw new InvalidOperationException("Unable to determine the database engine from the connection string.");
        }

        private static bool HasAny(string value, params string[] indicators)
            => indicators.Any(indicator => Contains(value, indicator));

        private static bool Contains(string value, string indicator)
            => value.IndexOf(indicator, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
