using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace CoverGo.Samples.Infrastructure.Authentication;

/// <summary>
/// Attaches a CoverGo bearer token to every outgoing request, acquiring it with the
/// OAuth2 client_credentials grant and caching it until it is close to expiry.
/// </summary>
/// <remarks>
/// Tokens live 600 seconds. Fetching one per call would triple the request count of a
/// migration and rate-limit the token endpoint, so one token serves a whole batch. The
/// refresh is guarded by a semaphore: without it, a burst of concurrent requests on a
/// cold or expired cache would each start their own token fetch.
/// </remarks>
public sealed class CoverGoTokenHandler(
    IHttpClientFactory httpClientFactory,
    CoverGoOptions options,
    ILogger<CoverGoTokenHandler> logger) : DelegatingHandler
{
    /// <summary>Refresh this far before actual expiry, so a token cannot die mid-flight.</summary>
    private static readonly TimeSpan ExpiryMargin = TimeSpan.FromSeconds(30);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _token;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer", await GetTokenAsync(cancellationToken));

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (_token is not null && DateTimeOffset.UtcNow < _expiresAt - ExpiryMargin)
        {
            return _token;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            // Re-check: another caller may have refreshed while this one waited.
            if (_token is not null && DateTimeOffset.UtcNow < _expiresAt - ExpiryMargin)
            {
                return _token;
            }

            (string token, TimeSpan lifetime) = await RequestTokenAsync(cancellationToken);
            _token = token;
            _expiresAt = DateTimeOffset.UtcNow + lifetime;

            logger.LogInformation(
                "Acquired a CoverGo token for tenant {TenantId}, valid for {Seconds}s",
                options.TenantId, (int)lifetime.TotalSeconds);

            return _token;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<(string Token, TimeSpan Lifetime)> RequestTokenAsync(
        CancellationToken cancellationToken)
    {
        Uri endpoint = options.TokenEndpoint
            ?? throw new InvalidOperationException("CoverGo:TokenEndpoint is not configured.");

        Dictionary<string, string> form = new()
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = options.ClientId,
        };

        // Only clients configured to require one send a secret.
        if (!string.IsNullOrEmpty(options.ClientSecret))
        {
            form["client_secret"] = options.ClientSecret;
        }

        using HttpClient client = httpClientFactory.CreateClient(nameof(CoverGoTokenHandler));
        using HttpResponseMessage response = await client.PostAsync(
            endpoint, new FormUrlEncodedContent(form), cancellationToken);

        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // The body names the problem (invalid_client, invalid_scope). Never log the form,
            // which carries the secret.
            throw new InvalidOperationException(
                $"Token request to {endpoint} failed with {(int)response.StatusCode}: {body}");
        }

        using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(body);

        string token = document.RootElement.TryGetProperty("access_token", out var t)
            ? t.GetString() ?? throw new InvalidOperationException("access_token was null.")
            : throw new InvalidOperationException($"No access_token in the response: {body}");

        int seconds = document.RootElement.TryGetProperty("expires_in", out var e)
            ? e.GetInt32()
            : 600;

        return (token, TimeSpan.FromSeconds(seconds));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _gate.Dispose();
        }

        base.Dispose(disposing);
    }
}
