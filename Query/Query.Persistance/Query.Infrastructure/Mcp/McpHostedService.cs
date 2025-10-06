using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Query.Infrastructure.Mcp
{
    public sealed class McpHostedService : BackgroundService
    {
        private readonly IReadOnlyDictionary<string, IMcpTool> _tools;
        private readonly JsonSerializerOptions _serializerOptions;

        public McpHostedService(IEnumerable<IMcpTool> tools)
        {
            if (tools is null)
            {
                throw new ArgumentNullException(nameof(tools));
            }

            _tools = tools.ToDictionary(tool => tool.Name, StringComparer.OrdinalIgnoreCase);
            _serializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var input = new StreamReader(Console.OpenStandardInput(), leaveOpen: true);
            using var output = new StreamWriter(Console.OpenStandardOutput())
            {
                AutoFlush = true
            };

            while (!stoppingToken.IsCancellationRequested)
            {
                string? line;
                try
                {
                    line = await input.ReadLineAsync().ConfigureAwait(false);
                }
                catch
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250), stoppingToken).ConfigureAwait(false);
                    continue;
                }

                if (line is null)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250), stoppingToken).ConfigureAwait(false);
                    continue;
                }

                JsonElement? idElement = null;
                JsonElement? paramsElement = null;
                string? method = null;

                try
                {
                    using var document = JsonDocument.Parse(line);
                    var root = document.RootElement;
                    if (root.TryGetProperty("method", out var methodProperty))
                    {
                        method = methodProperty.GetString();
                    }

                    if (root.TryGetProperty("id", out var idProperty))
                    {
                        idElement = idProperty.Clone();
                    }

                    if (root.TryGetProperty("params", out var paramsProperty))
                    {
                        paramsElement = paramsProperty.Clone();
                    }
                }
                catch (JsonException)
                {
                    await WriteErrorAsync(output, idElement, -32700, "Parse error").ConfigureAwait(false);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(method))
                {
                    await WriteErrorAsync(output, idElement, -32600, "Invalid request").ConfigureAwait(false);
                    continue;
                }

                if (!_tools.TryGetValue(method, out var tool))
                {
                    await WriteErrorAsync(output, idElement, -32601, "Method not found").ConfigureAwait(false);
                    continue;
                }

                try
                {
                    var result = await tool.ExecuteAsync(paramsElement, stoppingToken).ConfigureAwait(false);
                    await WriteResponseAsync(output, idElement, result).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    await WriteErrorAsync(output, idElement, -32603, ex.Message).ConfigureAwait(false);
                }
            }
        }

        private Task WriteResponseAsync(StreamWriter output, JsonElement? id, object? result)
        {
            if (id is null)
            {
                return Task.CompletedTask;
            }

            var response = new JsonRpcResponse
            {
                Result = result,
                Id = ConvertId(id.Value)
            };

            var json = JsonSerializer.Serialize(response, _serializerOptions);
            return output.WriteLineAsync(json);
        }

        private Task WriteErrorAsync(StreamWriter output, JsonElement? id, int code, string message)
        {
            if (id is null)
            {
                return Task.CompletedTask;
            }

            var response = new JsonRpcResponse
            {
                Error = new JsonRpcError(code, message),
                Id = ConvertId(id.Value)
            };

            var json = JsonSerializer.Serialize(response, _serializerOptions);
            return output.WriteLineAsync(json);
        }

        private static object? ConvertId(JsonElement id)
            => id.ValueKind switch
            {
                JsonValueKind.String => id.GetString(),
                JsonValueKind.Number => id.TryGetInt64(out var longValue) ? longValue : id.GetDouble(),
                JsonValueKind.Null => null,
                _ => id.GetRawText()
            };

        private sealed class JsonRpcResponse
        {
            public string Jsonrpc { get; set; } = "2.0";

            public object? Result { get; set; }

            public JsonRpcError? Error { get; set; }

            public object? Id { get; set; }
        }

        private sealed record JsonRpcError(int Code, string Message);
    }
}
