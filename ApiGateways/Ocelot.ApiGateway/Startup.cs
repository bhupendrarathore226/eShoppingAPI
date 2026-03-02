using System.Threading.Tasks;
using Common.Logging;
using Common.Logging.Correlation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MMLib.SwaggerForOcelot.DependencyInjection;
using MMLib.SwaggerForOcelot.Middleware;
using Ocelot.Cache.CacheManager;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Kubernetes;
using Ocelot.Provider;
using Ocelot.Provider.Polly;

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

        // MVC infrastructure is required so Swashbuckle can generate documents
        // (it provides IApiDescriptionGroupCollectionProvider, etc.).
        services.AddControllers();

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
                // Accept HTTP (non-HTTPS) Identity Server in development environments only.
                options.RequireHttpsMetadata = !_env.IsDevelopment();
            });

        var ocelotBuilder = services.AddOcelot()
            .AddPolly()
            .AddCacheManager(o => o.WithDictionaryHandle());

        if (_env.IsProduction())
        {
            ocelotBuilder.AddKubernetes();
        }

        services.AddSwaggerForOcelot(_configuration, opts =>
        {
            // Do not expose a gateway-level swagger doc; downstream docs are sufficient.
            opts.GenerateDocsForGatewayItSelf = false;
        });
    }

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
            endpoints.MapControllers();
            endpoints.MapGet("/", async context => { await context.Response.WriteAsync("EShopping API Gateway"); });
        });

        var swaggerSection = _configuration.GetSection("SwaggerGateway");
        var exposeSwaggerUi = swaggerSection.GetValue("ExposeSwaggerUI", !env.IsProduction());
        if (exposeSwaggerUi)
        {
            var routePrefix = swaggerSection.GetValue<string>("RoutePrefix") ?? "gateway/docs";
            var documentTitle = swaggerSection.GetValue<string>("DocumentTitle") ?? "EShopping API Gateway";

            app.UseSwaggerForOcelotUI(opt =>
            {
                opt.RoutePrefix = routePrefix;
                opt.DocumentTitle = documentTitle;
                // MMLib.SwaggerForOcelot serves individual service JSON docs at
                // {PathToSwaggerGenerator}/{version}/{key}  e.g. /swagger/docs/v1/catalog
                opt.PathToSwaggerGenerator = "/swagger/docs";
            });
        }

        app.UseOcelot().Wait();
    }
}