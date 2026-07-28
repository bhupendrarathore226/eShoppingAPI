// --- Bring in the tools/libraries this file needs ---
using EventBus.Messages.Common;         // Has the queue name constants (e.g. "basket-checkout")
using HealthChecks.UI.Client;           // Formats the /health response as nice JSON
using MassTransit;                      // The message-bus library that talks to RabbitMQ
using Microsoft.ApplicationInsights;   // Azure monitoring SDK (unused directly here but required by telemetry setup)
using Microsoft.ApplicationInsights.Extensibility; // Same as above
using Microsoft.AspNetCore.Diagnostics;            // Gives access to exception details inside the error handler
using Microsoft.AspNetCore.Diagnostics.HealthChecks; // HealthCheckOptions used in Configure()
using Microsoft.OpenApi.Models;         // Lets us describe the API in Swagger (title, version, etc.)
using Ordering.API.EventBusConsumer;    // Our own classes that process incoming queue messages
using Ordering.Application.Extensions; // Extension method: registers application-layer services (handlers, validators)
using Ordering.Infrastructure.Data;    // OrderContext — the Entity Framework database context
using Ordering.Infrastructure.Extensions; // Extension method: registers infrastructure services (DB connection, repos)

namespace Ordering.API;

// Startup is the "configuration hub" of the app.
// ASP.NET Core calls two methods automatically:
//   1. ConfigureServices  — tell the app WHAT services exist (dependency injection container)
//   2. Configure          — tell the app HOW requests flow through the pipeline (middleware)
public class Startup
{
    // Constructor: ASP.NET Core injects IConfiguration (appsettings.json values) for us.
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    // Expose the config so we can read it anywhere in this class.
    public IConfiguration Configuration { get; }

    // ─────────────────────────────────────────────────────────────
    // ConfigureServices — Register everything the app needs.
    // Think of this as writing a shopping list of services so that
    // any class can ask for them later via constructor injection.
    // ─────────────────────────────────────────────────────────────
    public void ConfigureServices(IServiceCollection services)
    {
        // Enable MVC controllers so our API endpoints work.
        services.AddControllers();

        // Allow the API to support multiple versions (v1, v2, …).
        services.AddApiVersioning();

        // Register application-layer services defined in Ordering.Application
        // (e.g. MediatR command/query handlers, FluentValidation validators).
        services.AddApplicationServices();

        // Register infrastructure-layer services defined in Ordering.Infrastructure
        // (e.g. database connection string, repositories).
        services.AddInfraServices(Configuration);

        // Register AutoMapper so we can easily convert between
        // domain models and DTOs (data transfer objects).
        services.AddAutoMapper(typeof(Startup));

        // Register the four RabbitMQ message consumers as scoped services.
        // "Scoped" means a new instance is created per HTTP request / message.
        services.AddScoped<BasketOrderingConsumer>();       // Processes checkout messages (v1)
        services.AddScoped<BasketOrderingConsumerV2>();     // Processes checkout messages (v2 — newer format)
        services.AddScoped<BasketOrderingConsumerFault>();  // Handles failed v1 messages (fault queue)
        services.AddScoped<BasketOrderingConsumerV2Fault>(); // Handles failed v2 messages (fault queue)

        // Set up Swagger — a browser UI that documents all our API endpoints.
        services.AddSwaggerGen(c =>
        {
            // Create a Swagger document called "v1" with this title.
            c.SwaggerDoc("v1", new OpenApiInfo {Title = "Ordering.API", Version = "v1"});
        });

        // Register health checks (used by Kubernetes / load balancers to know if the app is alive)
        // and also register the EF Core database context so it can be injected where needed.
        services.AddHealthChecks().Services.AddDbContext<OrderContext>();

        // Send telemetry (logs, metrics, exceptions) to Azure Application Insights.
        services.AddApplicationInsightsTelemetry();

        // Read the RabbitMQ connection details (host address) from appsettings.json.
        var eventBusSettings = Configuration.GetSection("EventBusSettings").Get<EventBusSettings>();

        // Set up MassTransit — the library that connects our app to RabbitMQ.
        services.AddMassTransit(config =>
        {
            // Tell MassTransit which consumer classes exist so it can wire them up.
            config.AddConsumer<BasketOrderingConsumer>();
            config.AddConsumer<BasketOrderingConsumerV2>();
            config.AddConsumer<BasketOrderingConsumerFault>();
            config.AddConsumer<BasketOrderingConsumerV2Fault>();

            // Use RabbitMQ as the message broker.
            config.UsingRabbitMq((ctx, cfg) =>
            {
                // Connect to the RabbitMQ server using the address from config.
                cfg.Host(eventBusSettings!.HostAddress);

                // Allow messages to be scheduled for delivery at a future time.
                cfg.UseDelayedMessageScheduler();

                // Circuit Breaker: if too many messages fail in a short window,
                // temporarily stop processing to avoid hammering a broken service.
                cfg.UseCircuitBreaker(cb =>
                {
                    cb.TrackingPeriod  = TimeSpan.FromMinutes(1);  // Look at the last 1 minute of traffic.
                    cb.TripThreshold   = 15;   // If 15% of messages fail → open the circuit (stop processing).
                    cb.ActiveThreshold = 10;   // Need at least 10 messages before the % matters.
                    cb.ResetInterval   = TimeSpan.FromMinutes(5);  // Try again after 5 minutes.
                });

                // ── Queue 1: BasketCheckoutQueue (v1) ──────────────────────
                // This queue receives "customer checked out" events from the Basket service.
                cfg.ReceiveEndpoint(EventBusConstants.BasketCheckoutQueue, c =>
                {
                    c.PrefetchCount          = 8;  // Fetch up to 8 messages at once from RabbitMQ.
                    c.ConcurrentMessageLimit = 4;  // But only process 4 of them at the same time.

                    // Delayed Redelivery: if a message fails due to a bigger infra problem,
                    // put it back on the queue and try again much later.
                    c.UseDelayedRedelivery(r =>
                        r.Intervals(
                            TimeSpan.FromMinutes(10),  // 1st retry: wait 10 minutes
                            TimeSpan.FromMinutes(30),  // 2nd retry: wait 30 minutes
                            TimeSpan.FromHours(1)));   // 3rd retry: wait 1 hour

                    // Immediate Retry (exponential backoff): for quick transient errors
                    // (e.g. DB briefly unavailable), retry faster before giving up.
                    c.UseMessageRetry(r =>
                        r.Exponential(5,                        // Up to 5 retries
                            TimeSpan.FromSeconds(1),            // Start waiting 1 second
                            TimeSpan.FromSeconds(60),           // Max wait of 60 seconds
                            TimeSpan.FromSeconds(5)));          // Multiply wait by 5 each time

                    // Tell this endpoint which consumer classes handle the messages.
                    c.ConfigureConsumer<BasketOrderingConsumer>(ctx);      // Happy-path consumer
                    c.ConfigureConsumer<BasketOrderingConsumerFault>(ctx); // Fault consumer (handles errors)
                });

                // ── Queue 2: BasketCheckoutQueueV2 (v2) ────────────────────
                // Same as Queue 1 but for a newer message format (V2).
                // Kept separate so old and new clients can run side-by-side.
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

        // CORS policy: allow any website/domain to call this API.
        // (Fine for development; tighten this in production for security.)
        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy",
                policy => { policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin(); });
        });
    }

    // ─────────────────────────────────────────────────────────────
    // Configure — Define the HTTP request pipeline (middleware).
    // Every incoming request travels through these steps IN ORDER,
    // like a conveyor belt. Each piece of middleware can inspect,
    // modify, or short-circuit the request.
    // ─────────────────────────────────────────────────────────────
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
    {
        // STEP 1 — Global error handler.
        // If any unhandled exception occurs anywhere below, catch it here,
        // log it, and return a clean JSON error response instead of a crash page.
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                context.Response.StatusCode = 500;                    // HTTP 500 = Internal Server Error
                context.Response.ContentType = "application/json";    // Tell the caller it's JSON
                var error = context.Features.Get<IExceptionHandlerFeature>(); // Get the actual exception
                var logger = loggerFactory.CreateLogger("GlobalExceptionHandler");
                if (error != null)
                    logger.LogError(error.Error, "Unhandled exception"); // Write the error to logs
                // Send a safe, generic message back to the caller (never expose internal details).
                await context.Response.WriteAsJsonAsync(new
                {
                    statusCode = 500,
                    message = "An unexpected error occurred. Please try again later."
                });
            });
        });

        // STEP 2 — In Development only: show a detailed error page in the browser.
        // This is useful while coding but must NEVER be on in production (leaks stack traces).
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // STEP 3 — Enable Swagger JSON endpoint.
        // NOTE: This is intentionally outside IsDevelopment() so the Ocelot API Gateway
        // can always fetch /swagger/v1/swagger.json to build its combined API docs,
        // regardless of which environment we are in.
        app.UseSwagger();

        // STEP 4 — Enable the Swagger browser UI (only in Development).
        // Visit /swagger in your browser to see and test all API endpoints interactively.
        if (env.IsDevelopment())
        {
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ordering.API v1"));
        }

        // STEP 5 — Enable routing so the framework can match URLs to controller actions.
        app.UseRouting();

        // STEP 6 — Check if the caller has permission to access the resource.
        // (Reads the JWT / bearer token from the request header.)
        app.UseAuthorization();

        // STEP 7 — Map URLs to actual handlers.
        app.UseEndpoints(endpoints =>
        {
            // Map all [Route] / [HttpGet] / [HttpPost] … attributes in controller classes.
            endpoints.MapControllers();

            // Expose a /health endpoint that returns detailed JSON about
            // whether the app and its dependencies (DB, etc.) are healthy.
            // Used by Kubernetes liveness/readiness probes and monitoring dashboards.
            endpoints.MapHealthChecks("/health", new HealthCheckOptions
            {
                Predicate = _ => true,                                      // Run ALL registered health checks
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse // Format as rich JSON
            });
        });
    }
}