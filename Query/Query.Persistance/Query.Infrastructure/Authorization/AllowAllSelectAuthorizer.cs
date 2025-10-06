using Query.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Query.Infrastructure.Authorization
{
    public sealed class AllowAllSelectAuthorizer : ISelectAuthorizer
    {
        public bool IsAllowed(string columnName) => true;
    }
}
