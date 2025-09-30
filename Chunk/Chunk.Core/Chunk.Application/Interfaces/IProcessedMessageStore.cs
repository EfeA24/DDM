using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chunk.Application.Interfaces
{
    public interface IProcessedMessageStore
    {
        Task<bool> ExistsAsync(string messageId, CancellationToken ct);
        Task MarkProcessedAsync(string messageId, CancellationToken ct);
    }
}
