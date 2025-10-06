using Query.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Query.Application.Models
{
    public record ConnectionInfo(string Name, string ConnectionString, DbEngine Engine);

}
