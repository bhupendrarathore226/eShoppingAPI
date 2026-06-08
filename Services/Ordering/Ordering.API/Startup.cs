using EventBus.Messages.Common;
using HealthChecks.UI.Client;
using MassTransit;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using Ordering.API.EventBusConsumer;
using Ordering.Application.Extensions;
using Ordering.Infrastructure.Data;
using Ordering.Infrastructure.Extensions;

namespace Ordering.API;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddApiVersioning();
        services.AddApplicationServices();
        services.AddInfraServices(Configuration);
        services.AddAutoMapper(typeof(Startup));
        services.AddScoped<BasketOrderingConsumer>();
        services.AddScoped<BasketOrderingConsumerV2>();
        services.AddScoped<BasketOrderingConsumerFault>();
        services.AddScoped<BasketOrderingConsumerV2Fault>();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo {Title = "Ordering.API", Version = "v1"});
        });
        services.AddHealthChecks().Services.AddDbContext<OrderContext>();
        services.AddApplicationInsightsTelemetry();
        var eventBusSettings = Configuration.GetSection("EventBusSettings").Get<EventBusSettings>();
        services.AddMassTransit(config =>
        {
            config.AddConsumer<BasketOrderingConsumer>();
            config.AddConsumer<BasketOrderingConsumerV2>();
            config.AddConsumer<BasketOrderingConsumerFault>();
            config.AddConsumer<BasketOrderingConsumerV2Fault>();
            config.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(eventBusSettings!.HostAddress);

                cfg.UseDelayedMessageScheduler();

                cfg.UseCircuitBreaker(cb =>
                {
                    cb.TrackingPeriod  = TimeSpan.FromMinutes(1);
                    cb.TripThreshold   = 15;
                    cb.ActiveThreshold = 10;
                    cb.ResetInterval   = TimeSpan.FromMinutes(5);
                });

                cfg.ReceiveEndpoint(EventBusConstants.BasketCheckoutQueue, c =>
                {
                    c.PrefetchCount          = 8;
                    c.ConcurrentMessageLimit = 4;
                    c.UseDelayedRedelivery(r =>
                        r.Intervals(
                            TimeSpan.FromMinutes(10),
                            TimeSpan.FromMinutes(30),
                            TimeSpan.FromHours(1)));
                    c.UseMessageRetry(r =>
                        r.Exponential(5,
                            TimeSpan.FromSeconds(1),
                            TimeSpan.FromSeconds(60),
                            TimeSpan.FromSeconds(5)));
                    c.ConfigureConsumer<BasketOrderingConsumer>(ctx);
                    c.ConfigureConsumer<BasketOrderingConsumerFault>(ctx);
                });

                cfg.ReceiveEndpoint(EventBusConstants.BasketCheckoutQueueV2, c =>
                {
                    c.PrefetchCount          = 8;
                    c.ConcurrentMessageLimit = 4;
                    c.UseDelayedRedelivery(r =>
                        r.Intervals(
                            TimeSpan.FromMinutes(10),
                            TimeSpan.FromMinutes(30),
                            TimeSpan.FromHours(1)));
                    c.UseMessageRetry(r =>
                        r.Exponential(5,
                            TimeSpan.FromSeconds(1),
                            TimeSpan.FromSeconds(60),
                            TimeSpan.FromSeconds(5)));
                    c.ConfigureConsumer<BasketOrderingConsumerV2>(ctx);
                    c.ConfigureConsumer<BasketOrderingConsumerV2Fault>(ctx);
                });
            });
        });
        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy",
                policy => { policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin(); });
        });
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

        // app.UseSwagger() is intentionally placed outside IsDevelopment().
        // The Ocelot API Gateway uses MMLib.SwaggerForOcelot to fetch
        // /swagger/v1/swagger.json from this service regardless of environment.
        app.UseSwagger();
        if (env.IsDevelopment())
        {
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ordering.API v1"));
        }

        app.UseRouting();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapHealthChecks("/health", new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
        });
    }
}