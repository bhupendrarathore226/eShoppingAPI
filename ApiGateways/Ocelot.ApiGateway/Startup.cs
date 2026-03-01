using Common.Logging;
using Common.Logging.Correlation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Ocelot.Cache.CacheManager;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Kubernetes;
using Ocelot.Provider.Polly;
using Swashbuckle.AspNetCore.SwaggerGen;

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

        // AddControllers registers IApiDescriptionGroupCollectionProvider and other
        // MVC infrastructure services that Swashbuckle's SwaggerGenerator depends on.
        // The gateway has no actual controllers, but these services must be registered.
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
                // Accept HTTP (non-HTTPS) Identity Server in development
                options.RequireHttpsMetadata = !_env.IsDevelopment();
            });

        // ── MMLib.SwaggerForOcelot: reads SwaggerEndPoints from config, fetches  ──
        // ── each downstream swagger.json, and transforms paths to gateway routes. ──
        // NOTE: AddSwaggerForOcelot internally calls AddSwaggerGen. Do NOT call
        //       AddSwaggerGen separately — it creates duplicate SwaggerGenerator
        //       registrations and breaks the DI container at startup.
        services.AddSwaggerForOcelot(_configuration);

        // Post-configure: add JWT Bearer security definition to the SwaggerGen
        // options registered by MMLib. This wires the Authorize button in Swagger UI.
        services.Configure<SwaggerGenOptions>(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "EShopping API Gateway",
                Version = "v1",
                Description = "Aggregated API documentation for all EShopping microservices routed through Ocelot."
            });

            var jwtScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter: Bearer {your_token}",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Id = JwtBearerDefaults.AuthenticationScheme,
                    Type = ReferenceType.SecurityScheme
                }
            };
            c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, jwtScheme);
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { jwtScheme, Array.Empty<string>() }
            });
        });

        var ocelotBuilder = services.AddOcelot()
            .AddCacheManager(o => o.WithDictionaryHandle())
            .AddPolly();

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

        // HttpsRedirection must be FIRST so the browser (and Swagger JS) follow
        // the redirect before any routing, CORS, or auth logic runs.
        app.UseHttpsRedirection();
        app.AddCorrelationIdMiddleware();
        app.UseRouting();
        app.UseCors("CorsPolicy");
        app.UseAuthentication();
        app.UseAuthorization();

        // ── Swagger UI: disabled in Production to prevent leaking API contracts ──
        if (!env.IsProduction())
        {
            app.UseSwagger();

            // UseSwaggerForOcelotUI MUST come before UseOcelot.
            // It intercepts /swagger/docs/{key} and serves the transformed,
            // path-mapped swagger JSON for each downstream microservice.
            app.UseSwaggerForOcelotUI(opt =>
            {
                opt.PathToSwaggerGenerator = "/swagger/docs";
            });
        }

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapGet("/", async context => { await context.Response.WriteAsync("EShopping API Gateway"); });
        });

        // UseOcelot must be last — it is a terminal middleware that proxies all
        // unmatched requests to the configured downstream services.
        app.UseOcelot().GetAwaiter().GetResult();
    }
}