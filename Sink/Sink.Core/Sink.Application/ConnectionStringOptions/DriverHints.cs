using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sink.Infrastructure.ConnectionStringOptions
{
    public static class DriverHints
    {
        public const string Npgsql = "Npgsql";
        public const string SqlClient = "SqlClient";
        public const string OracleManaged = "Oracle.ManagedDataAccess";
        public const string MySqlConnector = "MySqlConnector";
        public const string SystemSqlite = "System.Data.SQLite";
        public const string Snowflake = "Snowflake.Data";
        public const string Redshift = "Npgsql";
        public const string Db2 = "IBM.Data.DB2";
        public const string ClickHouse = "ClickHouse.Client";
        public const string Vertica = "Vertica.Client";
        public const string Hana = "Sap.Data.Hana";
        public const string Unknown = "Unknown";
    }
}
