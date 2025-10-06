using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sink.Infrastructure.ConnectionStringOptions
{
    public record ConnectionStringInterpretation(
        string Engine,
        string Family,
        string DriverHint,
        double Confidence,
        IReadOnlyList<string> Rationale,
        ConnectionStringParts Parts,
        IReadOnlyList<string> Warnings)
    {
        public static ConnectionStringInterpretation Unknown() => new(
            DatabaseEngines.Unknown,
            ConnectionFamilies.Unknown,
            DriverHints.Unknown,
            0.0,
            Array.Empty<string>(),
            ConnectionStringParts.Empty,
            Array.Empty<string>());
    }
}
