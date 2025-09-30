using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chunk.Domain.Enums
{
    public enum ChunkSetStatus
    {
        Pending = 0,
        InProgress = 1,
        Completed = 2,
        Failed = 3
    }
}
