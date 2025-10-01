using Chunk.Application.Interfaces;
using Chunk.Infrastructure.ChunkManager;
using Chunk.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chunk.Infrastructure.Mongo
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddChunkInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<MongoOptions>(configuration.GetSection(MongoOptions.SectionName));

            services.AddSingleton<IMongoClient>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
                if (string.IsNullOrWhiteSpace(options.ConnectionString))
                {
                    throw new InvalidOperationException("MongoDB connection string is not configured.");
                }

                return new MongoClient(options.ConnectionString);
            });

            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
                return new ChunkMongo(sp.GetRequiredService<IMongoClient>(), options.Database);
            });

            services.AddSingleton<IProcessedMessageStore, MongoProcessedMessageStore>();
            services.AddHostedService<ChunkOrchestrator>();

            return services;
        }
    }
}
