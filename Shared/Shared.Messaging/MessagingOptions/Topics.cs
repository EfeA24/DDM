using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Messaging.MessagingOptions
{
    public static class Topics
    {
        public const string IngestRaw = "ingest.raw";
        public const string IngestNormalized = "ingest.normalized";
        public const string ErrorsDlq = "errors.dlq";
    }
}
