using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Common.Logging.Apim;

/// <summary>
/// Rejects requests that carry APIM-injected identity headers (X-User-Id) but
/// do NOT originate from a configured trusted IP range (the APIM subnet).
/// 
/// This prevents header-spoofing attacks where a malicious caller bypasses APIM
/// and injects X-User-Id directly against the microservice endpoint.
/// 
/// Place this middleware BEFORE <see cref="ApimIdentityMiddleware"/>.
/// </summary>
public class ApimSubnetGuardMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApimSubnetGuardMiddleware> _logger;
    private readonly ApimSubnetGuardOptions _options;

    public ApimSubnetGuardMiddleware(
        RequestDelegate next,
        ILogger<ApimSubnetGuardMiddleware> logger,
        IOptions<ApimSubnetGuardOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only enforce when the guard is enabled (typically disabled for local dev)
        if (_options.Enabled && HasApimHeaders(context.Request))
        {
            var remoteIp = context.Connection.RemoteIpAddress;

            if (remoteIp is null || !IsFromTrustedApimSubnet(remoteIp))
            {
                _logger.LogWarning(
                    "Rejected spoofed APIM header from {RemoteIp}. " +
                    "X-User-Id={UserId}. Request: {Method} {Path}",
                    remoteIp?.ToString() ?? "unknown",
                    context.Request.Headers["X-User-Id"].FirstOrDefault() ?? "-",
                    context.Request.Method,
                    context.Request.Path);

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync(
                    """{"type":"https://eshopping.com/errors/forbidden","title":"Forbidden","status":403}""");
                return;
            }
        }

        await _next(context);
    }

    private static bool HasApimHeaders(HttpRequest request)
        => request.Headers.ContainsKey("X-User-Id")
        || request.Headers.ContainsKey("X-User-Roles");

    private bool IsFromTrustedApimSubnet(IPAddress remoteIp)
    {
        foreach (var cidr in _options.TrustedApimCidrs)
        {
            if (IsInCidr(remoteIp, cidr))
                return true;
        }
        return false;
    }

    private static bool IsInCidr(IPAddress address, string cidr)
    {
        var parts = cidr.Split('/');
        if (parts.Length != 2) return false;

        if (!IPAddress.TryParse(parts[0], out var networkAddress)) return false;
        if (!int.TryParse(parts[1], out var prefixLength)) return false;

        var networkBytes = networkAddress.GetAddressBytes();
        var addressBytes = address.MapToIPv4().GetAddressBytes();

        if (networkBytes.Length != addressBytes.Length) return false;

        var fullBytes  = prefixLength / 8;
        var extraBits  = prefixLength % 8;

        for (var i = 0; i < fullBytes; i++)
        {
            if (networkBytes[i] != addressBytes[i])
                return false;
        }

        if (extraBits > 0 && fullBytes < networkBytes.Length)
        {
            var mask = (byte)(0xFF << (8 - extraBits));
            if ((networkBytes[fullBytes] & mask) != (addressBytes[fullBytes] & mask))
                return false;
        }

        return true;
    }
}

/// <summary>Configuration model for <see cref="ApimSubnetGuardMiddleware"/>.</summary>
public class ApimSubnetGuardOptions
{
    /// <summary>
    /// Set to false in Development to allow direct service calls without APIM.
    /// Always true in Production.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// List of CIDR ranges that are allowed to inject APIM headers.
    /// Example: ["10.0.1.0/24"] — the APIM subnet.
    /// </summary>
    public List<string> TrustedApimCidrs { get; set; } = new();
}
