using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Contracts.Ingest
{
    public record ChunkInfo(int Index, int Total, int Size);

}
