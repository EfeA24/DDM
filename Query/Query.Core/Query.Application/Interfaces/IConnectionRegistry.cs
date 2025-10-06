using Query.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Query.Application.Interfaces
{
    public interface IConnectionRegistry
    {
        ConnectionInfo Get(string name);
    }
}
