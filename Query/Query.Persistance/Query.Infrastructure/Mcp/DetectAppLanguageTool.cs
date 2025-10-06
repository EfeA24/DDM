using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Query.Infrastructure.Mcp
{
    public sealed class DetectAppLanguageTool : IMcpTool
    {
        public string Name => "detectAppLanguage";

        public Task<object?> ExecuteAsync(JsonElement? @params, CancellationToken cancellationToken)
        {
            var result = new
            {
                dotNetVersion = Environment.Version.ToString(),
                targetFramework = "net8.0",
                projectType = "ASP.NET Core",
                odata = new
                {
                    version = "4.0",
                    enabled = true
                }
            };

            return Task.FromResult<object?>(result);
        }
    }
}
