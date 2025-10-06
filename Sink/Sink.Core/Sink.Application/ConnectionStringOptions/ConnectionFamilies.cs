using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sink.Infrastructure.ConnectionStringOptions
{
    public static class ConnectionFamilies
    {
        public const string Native = "Native";
        public const string Odbc = "ODBC";
        public const string OleDb = "OLEDB";
        public const string Jdbc = "JDBC";
        public const string FileDb = "FileDB";
        public const string Unknown = "Unknown";
    }
}
