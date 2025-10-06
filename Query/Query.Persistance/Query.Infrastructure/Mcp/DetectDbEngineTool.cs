using Query.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Query.Infrastructure.Mcp
{
    public sealed class DetectDbEngineTool : IMcpTool
    {
        private readonly IConnectionRegistry _connectionRegistry;

        public DetectDbEngineTool(IConnectionRegistry connectionRegistry)
            => _connectionRegistry = connectionRegistry ?? throw new ArgumentNullException(nameof(connectionRegistry));

        public string Name => "detectDbEngine";

        public Task<object?> ExecuteAsync(JsonElement? @params, CancellationToken cancellationToken)
        {
            var connection = _connectionRegistry.Get("Default");
            var result = new
            {
                engine = connection.Engine.ToString()
            };

            return Task.FromResult<object?>(result);
        }
    }
}
