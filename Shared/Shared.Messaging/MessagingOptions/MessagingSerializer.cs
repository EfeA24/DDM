using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Shared.Messaging.MessagingOptions
{
    public static class MessagingSerializer
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, SerializerOptions);

        public static T? Deserialize<T>(string value) => JsonSerializer.Deserialize<T>(value, SerializerOptions);
    }
}
