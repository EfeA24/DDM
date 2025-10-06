using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sink.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddSinkApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<SinkRouterOptions>(configuration.GetSection("Sink:Routing"));
            services.AddSingleton<ISinkRouter, SinkRouter>();
            return services;
        }
    }
}
