using Microsoft.AspNetCore.OData;
using Microsoft.Data.Edm;
using Query.Api.OData;
using Query.Application.Interfaces;
using Query.Application.Options;
using Query.Application.Services;
using Query.Infrastructure.Authorization;
using Query.Infrastructure.Caching;
using Query.Infrastructure.Configuration;
using Query.Infrastructure.Mcp;
using Query.Infrastructure.Providers;
using Query.Infrastructure.Sql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<ConnectionRegistryOptions>()
    .Bind(builder.Configuration.GetSection(ConnectionRegistryOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<CacheOptions>()
    .Bind(builder.Configuration.GetSection(CacheOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var edmModel = DynamicEdmModelFactory.CreateModel();
builder.Services.AddSingleton(edmModel);

builder.Services.AddControllers().AddOData(options =>
{
    options.Select().Filter().OrderBy().Count().SetMaxTop(null).SkipToken();
    options.AddRouteComponents("odata", edmModel);
});

builder.Services.AddSingleton<IConnectionRegistry, ConnectionRegistry>();
builder.Services.AddSingleton<ISelectAuthorizer, AllowAllSelectAuthorizer>();
builder.Services.AddSingleton<SqlBuilder>();
builder.Services.AddSingleton<IRedisCache, NoOpRedisCache>();
builder.Services.AddSingleton<IMongoAggregate, NoOpMongoAggregate>();
builder.Services.AddScoped<IReadProviderAdapter, PostgresReadAdapter>();
builder.Services.AddScoped<IReadProviderAdapter, SqlServerReadAdapter>();
builder.Services.AddScoped<IReadProviderAdapter, OracleReadAdapter>();
builder.Services.AddScoped<ExecutionPlanner>();
builder.Services.AddSingleton<IMcpTool, DetectAppLanguageTool>();
builder.Services.AddSingleton<IMcpTool, DetectDbEngineTool>();
builder.Services.AddHostedService<McpHostedService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();
