using Microsoft.AspNetCore.Http;

namespace Common.Logging.Apim;

/// <summary>
/// Reads the trusted identity headers that Azure APIM injects after JWT validation
/// (X-User-Id, X-User-Roles, X-User-Email) and exposes them via <see cref="ApimContext"/>.
/// 
/// SECURITY NOTE:
/// These headers must only be trusted when the request arrives from the APIM subnet.
/// The companion <see cref="ApimSubnetGuardMiddleware"/> rejects requests that carry
/// X-User-Id but do NOT originate from the configured trusted APIM CIDR range.
/// Never call AddApimIdentityMiddleware without also calling AddApimSubnetGuard.
/// </summary>
public class ApimIdentityMiddleware
{
    private readonly RequestDelegate _next;

    public ApimIdentityMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IApimRequestContext apimContext)
    {
        // APIM sets these headers after successfully validating the JWT.
        // The raw Authorization header has been stripped by the APIM policy.
        var userId    = context.Request.Headers["X-User-Id"].FirstOrDefault();
        var userRoles = context.Request.Headers["X-User-Roles"].FirstOrDefault();
        var userEmail = context.Request.Headers["X-User-Email"].FirstOrDefault();

        apimContext.UserId    = userId    ?? string.Empty;
        apimContext.UserEmail = userEmail ?? string.Empty;
        apimContext.UserRoles = string.IsNullOrEmpty(userRoles)
            ? Array.Empty<string>()
            : userRoles.Split(',', StringSplitOptions.RemoveEmptyEntries);

        await _next(context);
    }
}
