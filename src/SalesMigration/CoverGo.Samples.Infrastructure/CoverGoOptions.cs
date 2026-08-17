using System.ComponentModel.DataAnnotations;

namespace CoverGo.Samples.Infrastructure;

/// <summary>
/// Everything a sample needs to reach a CoverGo tenant. Bound from appsettings.json,
/// environment variables and user secrets, in that order of increasing precedence.
/// </summary>
/// <remarks>
/// <see cref="ClientSecret"/> is deliberately absent from appsettings.json. Supply it
/// through <c>dotnet user-secrets</c> locally, or the environment
/// (<c>CoverGo__ClientSecret</c>) elsewhere. Never commit it.
/// </remarks>
public sealed class CoverGoOptions
{
    public const string SectionName = "CoverGo";

    /// <summary>The tenant the token is issued for. Calls auto-scope to it; never sent as an argument.</summary>
    [Required(AllowEmptyStrings = false)]
    public string TenantId { get; init; } = string.Empty;

    /// <summary>OAuth2 token endpoint for the client_credentials grant.</summary>
    [Required]
    public Uri? TokenEndpoint { get; init; }

    /// <summary>V1 GraphQL gateway. Serves every operation the migration samples call.</summary>
    [Required]
    public Uri? GatewayV1Url { get; init; }

    /// <summary>V2 GraphQL gateway. Serves quotation, payments and the other V2 subgraphs.</summary>
    [Required]
    public Uri? GatewayV2Url { get; init; }

    /// <summary>Base address for the REST file endpoint used to upload document binaries.</summary>
    [Required]
    public Uri? FilesBaseUrl { get; init; }

    [Required(AllowEmptyStrings = false)]
    public string ClientId { get; init; } = string.Empty;

    /// <summary>Optional: only clients configured to require one need it.</summary>
    public string? ClientSecret { get; init; }
}
