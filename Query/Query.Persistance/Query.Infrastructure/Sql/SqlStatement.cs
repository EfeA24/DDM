using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Query.Infrastructure.Sql
{
    public record SqlStatement(string Sql, DynamicParameters Parameters);

}
