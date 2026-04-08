using Common.Logging.Correlation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Common.Logging.Apim;

/// <summary>
/// Extension methods to register and configure all APIM-related middleware in one call.
/// 
/// Usage in each microservice's Startup / Program:
///   services.AddApimIntegration(configuration);
///   ...
///   app.UseApimIntegration();   // call before UseAuthentication / UseAuthorization
/// </summary>
public static class ApimExtensions
{
    /// <summary>
    /// Registers:
    ///   • ForwardedHeaders middleware options (so X-Forwarded-For is trusted from APIM subnet)
    ///   • ApimSubnetGuardOptions (loaded from "ApimSettings" config section)
    ///   • IApimRequestContext as a scoped service
    /// </summary>
    public static IServiceCollection AddApimIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var apimSettings = configuration.GetSection("ApimSettings");

        // ForwardedHeaders: trust only the APIM subnet as a known proxy
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor  |
                ForwardedHeaders.XForwardedProto |
                ForwardedHeaders.XForwardedHost;

            // Restrict to APIM subnet — prevents header spoofing from arbitrary proxies
            var cidrs = apimSettings.GetSection("TrustedApimCidrs")
                                    .Get<List<string>>() ?? new List<string>();
            foreach (var cidr in cidrs)
            {
                var parts = cidr.Split('/');
                if (parts.Length == 2
                    && System.Net.IPAddress.TryParse(parts[0], out var ip)
                    && int.TryParse(parts[1], out var prefix))
                {
                    options.KnownNetworks.Add(
                        new Microsoft.AspNetCore.HttpOverrides.IPNetwork(ip, prefix));
                }
            }

            options.ForwardLimit = 1; // Only one proxy hop (APIM)
        });

        // Subnet guard options
        services.Configure<ApimSubnetGuardOptions>(apimSettings);
        services.AddScoped<IApimRequestContext, ApimRequestContext>();

        return services;
    }

    /// <summary>
    /// Inserts the APIM middleware pipeline in the correct order:
    ///   1. UseForwardedHeaders (process X-Forwarded-* before anything else)
    ///   2. ApimSubnetGuardMiddleware (reject spoofed headers from non-APIM IPs)
    ///   3. CorrelationIdMiddleware (already exists — kept as-is)
    ///   4. ApimIdentityMiddleware (populate IApimRequestContext from headers)
    /// </summary>
    public static IApplicationBuilder UseApimIntegration(this IApplicationBuilder app)
    {
        app.UseForwardedHeaders();
        app.UseMiddleware<ApimSubnetGuardMiddleware>();
        app.AddCorrelationIdMiddleware();           // existing extension from Common.Logging
        app.UseMiddleware<ApimIdentityMiddleware>();
        return app;
    }
}
