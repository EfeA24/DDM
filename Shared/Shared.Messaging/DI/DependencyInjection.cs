using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Messaging.KafkaOptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Messaging.DI
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddSharedMessaging(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<KafkaManageOptions>(configuration.GetSection(KafkaManageOptions.SectionName));
            services.AddSingleton<IKafkaProducer, KafkaProducer>();
            services.AddSingleton<IKafkaConsumerFactory, KafkaConsumerFactory>();
            return services;
        }
    }
}
