using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Messaging;
using Shared.Messaging.KafkaOptions;
using StackExchange.Redis;
using Shared.Messaging.DI;

namespace Shared.Infrastructure.DI
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSharedMessaging(configuration);
            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var connectionString = configuration.GetConnectionString("Redis");
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException("Redis connection string is not configured.");
                }

                return ConnectionMultiplexer.Connect(connectionString);
            });

            return services;
        }
    }
}
