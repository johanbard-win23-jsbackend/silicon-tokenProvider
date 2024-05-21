using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using silicon_tokenProvider.Infrastructure.Data.Contexts;
using silicon_tokenProvider.Infrastructure.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddDbContextFactory<DataContext>(options =>
        {
            options.UseSqlServer(Environment.GetEnvironmentVariable("TokenDatabase"));
        });

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ITokenGenerator, TokenGenerator>(); 
    })
    .Build();

host.Run();
