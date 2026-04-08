namespace Common.Logging.Apim;

/// <summary>
/// Scoped service that holds APIM-verified request context for the lifetime of
/// a single HTTP request. Inject this wherever you need the authenticated user's
/// identity without re-parsing the JWT.
/// </summary>
public interface IApimRequestContext
{
    /// <summary>JWT subject claim — typically the user's unique identifier (sub).</summary>
    string UserId { get; set; }

    /// <summary>Email claim extracted by APIM from the JWT.</summary>
    string UserEmail { get; set; }

    /// <summary>Role claims extracted by APIM from the JWT (comma-separated in header).</summary>
    string[] UserRoles { get; set; }

    /// <summary>Returns true if the user has at least one of the supplied roles.</summary>
    bool HasRole(params string[] roles);
}

/// <inheritdoc />
public class ApimRequestContext : IApimRequestContext
{
    public string   UserId    { get; set; } = string.Empty;
    public string   UserEmail { get; set; } = string.Empty;
    public string[] UserRoles { get; set; } = Array.Empty<string>();

    public bool HasRole(params string[] roles)
        => roles.Any(r => UserRoles.Contains(r, StringComparer.OrdinalIgnoreCase));
}
