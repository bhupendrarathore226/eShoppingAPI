using System.Reflection;
using Catalog.Application.Handlers;
using Catalog.Core.Repositories;
using Catalog.Infrastructure.Data;
using Catalog.Infrastructure.Repositories;
using Common.Logging;
using Common.Logging.Correlation;
using HealthChecks.UI.Client;
using MediatR;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;

namespace Catalog.API;

public class Startup
{
    public IConfiguration Configuration;

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddApiVersioning();
        var connectionString = Configuration.GetValue<string>("DatabaseSettings:ConnectionString");
        services.AddDbContext<CatalogContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
        services.AddHealthChecks()
            .AddMySql(connectionString, "Catalog MySQL Health Check", HealthStatus.Degraded);
        services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo {Title = "Catalog.API", Version = "v1"}); });
        
        //DI
        services.AddAutoMapper(typeof(Startup));
        services.AddMediatR(typeof(CreateProductHandler).GetTypeInfo().Assembly);
        services.AddScoped<ICorrelationIdGenerator, CorrelationIdGenerator>();
        services.AddScoped<ICatalogContext>(provider => provider.GetRequiredService<CatalogContext>());
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IBrandRepository, ProductRepository>();
        services.AddScoped<ITypesRepository, ProductRepository>();

        services.AddControllers();
        //Identity Server changes
        // var userPolicy = new AuthorizationPolicyBuilder()
        //     .RequireAuthenticatedUser()
        //     .Build();
        //
        // services.AddControllers(config =>
        // {
        //     config.Filters.Add(new AuthorizeFilter(userPolicy));
        // });
        //
        //
        // services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        //         .AddJwtBearer(options =>
        //         {
        //             options.Authority = "https://localhost:9009";
        //             options.Audience = "Catalog";
        //         });
        // services.AddAuthorization(options =>
        // {
        //     options.AddPolicy("CanRead", policy=>policy.RequireClaim("scope", "catalogapi.read"));
        // });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // app.UseSwagger() is intentionally placed outside IsDevelopment().
        // The Ocelot API Gateway uses MMLib.SwaggerForOcelot to fetch
        // /swagger/v1/swagger.json from this service regardless of environment.
        // app.UseSwaggerUI() remains dev-only to avoid leaking the interactive UI.
        app.UseSwagger();
        if (env.IsDevelopment())
        {
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog.API v1"));
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        // app.UseCors("CorsPolicy");
        app.UseAuthentication();
        app.UseStaticFiles();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHealthChecks("/health", new HealthCheckOptions()
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
        });
    }
}