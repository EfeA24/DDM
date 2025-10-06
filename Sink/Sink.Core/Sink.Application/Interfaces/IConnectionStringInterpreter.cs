using Sink.Infrastructure.ConnectionStringOptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sink.Application.Interfaces
{
    public interface IConnectionStringInterpreter
    {
        ConnectionStringInterpretation Interpret(string connectionString);

        string Mask(string connectionString);
    }
}
