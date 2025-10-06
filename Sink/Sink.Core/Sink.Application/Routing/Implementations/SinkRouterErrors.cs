using Share.Kernel.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sink.Application.Routing.Implementations
{
    public sealed record ConnectionStringNotFoundError()
        : Error("CONNECTION_STRING_NOT_FOUND", "Connection string could not be resolved.");

    public sealed record EphemeralConnectionStringNotAllowedError()
        : Error("EPHEMERAL_CONNECTION_STRING_DISABLED", "Ephemeral connection strings are disabled for this environment.");

    public sealed record EphemeralConnectionStringRejectedError(
        string Code,
        string Message,
        string Detail,
        string MaskedConnectionString) : Error(Code, Message);

    public sealed record UnsupportedEngineError(string Engine, string DriverHint)
        : Error("UNSUPPORTED_ENGINE", $"Engine '{Engine}' is not supported. Desteklenenler: SqlServer, PostgreSQL, Oracle")
    {
        public string Hint => "Desteklenenler: SqlServer, PostgreSQL, Oracle";
    }
}
