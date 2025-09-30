using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Share.Kernel.Tracing
{
    public static class Correlation
    {
        public const string HeaderCorrelationId = "X-Correlation-Id";

        public static string NewId() => Guid.NewGuid().ToString("N");
    }
}
