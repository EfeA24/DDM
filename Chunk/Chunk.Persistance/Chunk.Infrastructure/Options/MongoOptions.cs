using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chunk.Infrastructure.Options
{
    public class MongoOptions
    {
        public const string SectionName = "Mongo";
        public string ConnectionString { get; set; } = string.Empty;
        public string Database { get; set; } = "ddm";
    }
}
