using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Query.Infrastructure.Mcp
{
    public interface IMcpTool
    {
        string Name { get; }

        Task<object?> ExecuteAsync(JsonElement? @params, CancellationToken cancellationToken);
    }
}
