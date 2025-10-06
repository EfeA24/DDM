using Sink.Application.Interfaces;
using Sink.Application.Routing.Interfaces;
using Sink.Domain.Entities.Routing;
using Sink.Infrastructure.ConnectionStringOptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sink.Application.Routing.Implementations
{
    public sealed class SinkRouter : ISinkRouter
    {
        public const string ConnectionStringHeader = "X-Connection-String";

        private readonly IConnectionStringInterpreter _interpreter;
        private readonly IConnectionStringRegistry _registry;
        private readonly IReadOnlyDictionary<string, ISinkWriteAdapter> _adapters;
        private readonly SinkRouterOptions _options;
        private readonly ILogger<SinkRouter> _logger;

        public SinkRouter(
            IConnectionStringInterpreter interpreter,
            IConnectionStringRegistry registry,
            IEnumerable<ISinkWriteAdapter> adapters,
            IOptions<SinkRouterOptions> options,
            ILogger<SinkRouter> logger)
        {
            _interpreter = interpreter ?? throw new ArgumentNullException(nameof(interpreter));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

            _adapters = adapters?.ToDictionary(a => a.Engine, StringComparer.OrdinalIgnoreCase)
                ?? throw new ArgumentNullException(nameof(adapters));
        }

        public async Task<Result<SinkRoutingOutcome>> RouteAsync(SinkRoutingContext context, CancellationToken cancellationToken = default)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var headers = context.Headers ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var (connectionString, isEphemeral, source) = await ResolveConnectionStringAsync(context, headers, cancellationToken);
            if (connectionString is null)
            {
                return Result<SinkRoutingOutcome>.Failure(new ConnectionStringNotFoundError());
            }

            if (isEphemeral && !IsEphemeralAllowed(context))
            {
                return Result<SinkRoutingOutcome>.Failure(new EphemeralConnectionStringNotAllowedError());
            }

            var interpretation = _interpreter.Interpret(connectionString);
            _logger.LogDebug("Connection string interpreted from {Source}: {MaskedConnectionString} -> Engine {Engine} ({Confidence})", source, _interpreter.Mask(connectionString), interpretation.Engine, interpretation.Confidence);

            if (isEphemeral)
            {
                var securityFailure = EvaluateEphemeralSecurity(connectionString, interpretation);
                if (securityFailure is not null)
                {
                    return Result<SinkRoutingOutcome>.Failure(securityFailure);
                }
            }

            if (!_adapters.TryGetValue(interpretation.Engine, out var adapter))
            {
                return Result<SinkRoutingOutcome>.Failure(new UnsupportedEngineError(interpretation.Engine, interpretation.DriverHint));
            }

            return Result<SinkRoutingOutcome>.Success(new SinkRoutingOutcome(adapter, connectionString, interpretation, isEphemeral));
        }

        private bool IsEphemeralAllowed(SinkRoutingContext context)
        {
            return _options.AllowEphemeralConnectionString || context.IsEphemeralAllowedOverride;
        }

        private async Task<(string? ConnectionString, bool IsEphemeral, string Source)> ResolveConnectionStringAsync(
            SinkRoutingContext context,
            IReadOnlyDictionary<string, string> headers,
            CancellationToken cancellationToken)
        {
            if (TryGetHeaderValue(headers, ConnectionStringHeader, out var headerValue) && !string.IsNullOrWhiteSpace(headerValue))
            {
                return (headerValue, true, "header");
            }

            if (!string.IsNullOrWhiteSpace(context.ConnectionKey))
            {
                var fromRegistry = await _registry.GetAsync(context.ConnectionKey, cancellationToken);
                if (!string.IsNullOrWhiteSpace(fromRegistry))
                {
                    return (fromRegistry, false, $"registry:{context.ConnectionKey}");
                }
            }

            if (!string.IsNullOrWhiteSpace(_options.DefaultConnectionKey))
            {
                var fromRegistry = await _registry.GetAsync(_options.DefaultConnectionKey, cancellationToken);
                if (!string.IsNullOrWhiteSpace(fromRegistry))
                {
                    return (fromRegistry, false, $"registry:{_options.DefaultConnectionKey}");
                }
            }

            return (null, false, "none");
        }

        private Error? EvaluateEphemeralSecurity(string rawConnectionString, ConnectionStringInterpretation interpretation)
        {
            if (_options.DenylistKeywords is { Count: > 0 })
            {
                foreach (var keyword in _options.DenylistKeywords)
                {
                    if (string.IsNullOrWhiteSpace(keyword))
                    {
                        continue;
                    }

                    if (rawConnectionString.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        return new EphemeralConnectionStringRejectedError(
                            "EPHEMERAL_DENYLIST",
                            $"Ephemeral connection string includes forbidden keyword '{keyword}'.",
                            keyword,
                            _interpreter.Mask(rawConnectionString));
                    }
                }
            }

            if (_options.AllowedHosts is { Count: > 0 })
            {
                var host = interpretation.Parts.Host;
                if (string.IsNullOrWhiteSpace(host) || !_options.AllowedHosts.Any(h => string.Equals(h, host, StringComparison.OrdinalIgnoreCase)))
                {
                    return new EphemeralConnectionStringRejectedError(
                        "EPHEMERAL_HOST_DENIED",
                        "Ephemeral connection string host is not allowed.",
                        host ?? string.Empty,
                        _interpreter.Mask(rawConnectionString));
                }
            }

            return null;
        }

        private static bool TryGetHeaderValue(IReadOnlyDictionary<string, string> headers, string headerName, out string value)
        {
            foreach (var header in headers)
            {
                if (string.Equals(header.Key, headerName, StringComparison.OrdinalIgnoreCase))
                {
                    value = header.Value;
                    return true;
                }
            }

            value = string.Empty;
            return false;
        }
    }
}
