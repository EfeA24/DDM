using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Query.Application.Interfaces
{
    public interface ISelectAuthorizer
    {
        bool IsAllowed(string columnName);
    }
}
