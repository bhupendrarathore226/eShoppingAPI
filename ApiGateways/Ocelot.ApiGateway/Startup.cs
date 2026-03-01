using Common.Logging;
using Common.Logging.Correlation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ocelot.Cache.CacheManager;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Kubernetes;

namespace Ocelot.ApiGateway;

public class Startup
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;

    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
        _configuration = configuration;
        _env = env;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<ICorrelationIdGenerator, CorrelationIdGenerator>();

        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy",
                policy => { policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin(); });
        });

        const string authScheme = "EShoppingGatewayAuthScheme";
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(authScheme, options =>
            {
                options.Authority = _configuration["IdentityServer:Authority"];
                options.Audience = "EShoppingGateway";
                // Accept HTTP (non-HTTPS) Identity Server in development
                options.RequireHttpsMetadata = !_env.IsDevelopment();
            });

        var ocelotBuilder = services.AddOcelot()
            .AddCacheManager(o => o.WithDictionaryHandle());

        // Only load the Kubernetes service-discovery provider in Production.
        // In Development/Local the provider is not available and causes startup errors.
        if (_env.IsProduction())
        {
            ocelotBuilder.AddKubernetes();
        }
    }

    // NOTE: Host expects a synchronous Configure method; block on Ocelot's async initialization.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.AddCorrelationIdMiddleware();
        app.UseRouting();
        app.UseCors("CorsPolicy");
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/", async context => { await context.Response.WriteAsync("EShopping API Gateway"); });
        });
        app.UseOcelot().GetAwaiter().GetResult();
    }
}