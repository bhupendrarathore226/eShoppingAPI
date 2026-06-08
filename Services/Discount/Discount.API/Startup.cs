using System.Reflection;
using Common.Logging;
using Microsoft.AspNetCore.Diagnostics;
using Common.Logging.Correlation;
using Discount.API.Services;
using Discount.Application.Handlers;
using Discount.Core.Repositories;
using Discount.Infrastructure.Repositories;
using MediatR;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace Discount.API;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateDiscountCommandHandler).Assembly));
        services.AddScoped<ICorrelationIdGenerator, CorrelationIdGenerator>();
        services.AddScoped<IDiscountRepository, DiscountRepository>();
        services.AddAutoMapper(typeof(Startup));
        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy",
                policy => { policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin(); });
        });
        services.AddApplicationInsightsTelemetry();
        services.AddGrpc();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "application/json";
                var error = context.Features.Get<IExceptionHandlerFeature>();
                var logger = loggerFactory.CreateLogger("GlobalExceptionHandler");
                if (error != null)
                    logger.LogError(error.Error, "Unhandled exception");
                await context.Response.WriteAsJsonAsync(new
                {
                    statusCode = 500,
                    message = "An unexpected error occurred. Please try again later."
                });
            });
        });
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGrpcService<DiscountService>();
            endpoints.MapGet("/", async context =>
            {
                await context.Response.WriteAsync(
                    "Communication with gRPC endpoints must be made through a gRPC client.");
            });
        });
    }
}