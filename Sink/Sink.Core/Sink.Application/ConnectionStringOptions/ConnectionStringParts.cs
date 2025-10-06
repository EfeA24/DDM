using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sink.Infrastructure.ConnectionStringOptions
{
    public record ConnectionStringParts(string? Host, string? Port, string? Database, string? User, string? File)
    {
        public static readonly ConnectionStringParts Empty = new(null, null, null, null, null);

        public ConnectionStringParts WithHost(string? value) => this with { Host = value };
        public ConnectionStringParts WithPort(string? value) => this with { Port = value };
        public ConnectionStringParts WithDatabase(string? value) => this with { Database = value };
        public ConnectionStringParts WithUser(string? value) => this with { User = value };
        public ConnectionStringParts WithFile(string? value) => this with { File = value };
    }
}
