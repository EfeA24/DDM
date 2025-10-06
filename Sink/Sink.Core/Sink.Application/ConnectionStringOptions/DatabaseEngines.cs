using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sink.Infrastructure.ConnectionStringOptions
{
    public static class DatabaseEngines
    {
        public const string PostgreSql = "PostgreSQL";
        public const string SqlServer = "SqlServer";
        public const string Oracle = "Oracle";
        public const string MySql = "MySQL";
        public const string MariaDb = "MariaDB";
        public const string Sqlite = "SQLite";
        public const string Snowflake = "Snowflake";
        public const string Redshift = "Redshift";
        public const string Db2 = "DB2";
        public const string ClickHouse = "ClickHouse";
        public const string Vertica = "Vertica";
        public const string Hana = "HANA";
        public const string OdbcUnknown = "ODBC-Unknown";
        public const string JdbcUnknown = "JDBC-Unknown";
        public const string Unknown = "Unknown";
    }
}
