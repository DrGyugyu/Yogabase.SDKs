using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Yogabase.Lambda;

/// <summary>One application user of the Lambda's project.</summary>
public sealed class LambdaUser
{
    /// <summary>Stable subject identifier, the value a caller's <c>sub</c> claim carries.</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Email address, when the user has one.</summary>
    public string? Email { get; set; }

    /// <summary>Display username, when the user has one.</summary>
    public string? Username { get; set; }

    /// <summary>Whether the user has confirmed the email address on the record.</summary>
    public bool EmailConfirmed { get; set; }

    /// <summary>Whether the user is currently banned from signing in.</summary>
    public bool Banned { get; set; }

    /// <summary>
    /// Project-defined attributes carried on the user record. The runtime API accepts attributes
    /// on create and update but this release does not yet project them back, so this is
    /// <see langword="null"/> until it does.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Attributes { get; set; }

    /// <summary>
    /// Any field the runtime returned that this version does not model. The internal Lambda API is
    /// not a published contract, so an unknown field is kept rather than dropped or refused.
    /// </summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Additional { get; set; }
}

/// <summary>Fields of a newly created application user.</summary>
public sealed class LambdaUserCreate
{
    /// <summary>Email address to register.</summary>
    public string? Email { get; set; }

    /// <summary>Display username to register.</summary>
    public string? Username { get; set; }

    /// <summary>
    /// Initial password. It travels to the gateway over the sandbox's own egress and is never
    /// logged by this wrapper; do not put it in the Lambda's own log output either.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>Project-defined attributes to set on creation.</summary>
    public IReadOnlyDictionary<string, string>? Attributes { get; set; }
}

/// <summary>Fields of an application-user update. An absent field is left unchanged.</summary>
public sealed class LambdaUserUpdate
{
    /// <summary>Ban state to set, or <see langword="null"/> to leave it unchanged.</summary>
    public bool? Banned { get; set; }

    /// <summary>Project-defined attributes to merge, or <see langword="null"/> to leave them.</summary>
    public IReadOnlyDictionary<string, string>? Attributes { get; set; }
}

/// <summary>An access token issued to an application user.</summary>
public sealed class LambdaAccessToken
{
    /// <summary>The bearer token. Return it to the caller; never log or persist it.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>When the token stops being accepted.</summary>
    public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>
/// The project's application-user directory, over the runtime's pre-authenticated
/// <c>yogabase-users</c> client. A Lambda administers its own project's users; it never reaches
/// workforce identities, console seats, or another project.
/// </summary>
public sealed class LambdaUsers
{
    private readonly HttpClient client;

    internal LambdaUsers(HttpClient client)
    {
        this.client = client;
    }

    /// <summary>Reads one user, or <see langword="null"/> when the subject is unknown to this project.</summary>
    /// <param name="subject">The user's subject, a UUID; any spelling is normalised to the canonical form.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<LambdaUser?> GetAsync(string subject, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            LambdaRoutes.User(LambdaRoutes.CanonicalSubject(subject, nameof(subject))));
        var response = await LambdaTransport
            .SendAsync(client, request, cancellationToken, allowNotFound: true)
            .ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            response.Dispose();
            return null;
        }

        return await LambdaTransport.ReadJsonAsync<LambdaUser>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Creates one application user.</summary>
    public async Task<LambdaUser> CreateAsync(
        LambdaUserCreate user,
        CancellationToken cancellationToken = default)
    {
        if (user is null)
        {
            throw new ArgumentNullException(nameof(user));
        }
        if (user.Email is null && user.Username is null)
        {
            throw new ArgumentException("A new user needs at least an email or a username.", nameof(user));
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, LambdaRoutes.Users)
        {
            Content = JsonBody(user),
        };
        var response = await LambdaTransport.SendAsync(client, request, cancellationToken).ConfigureAwait(false);
        return await LambdaTransport.ReadJsonAsync<LambdaUser>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Updates one application user's ban state or attributes.</summary>
    public async Task<LambdaUser> UpdateAsync(
        string subject,
        LambdaUserUpdate update,
        CancellationToken cancellationToken = default)
    {
        if (update is null)
        {
            throw new ArgumentNullException(nameof(update));
        }
        if (update.Banned is null && update.Attributes is null)
        {
            throw new ArgumentException("An update must change the ban state or the attributes.", nameof(update));
        }

        using var request = new HttpRequestMessage(
            new HttpMethod("PATCH"),
            LambdaRoutes.User(LambdaRoutes.CanonicalSubject(subject, nameof(subject))))
        {
            Content = JsonBody(update),
        };
        var response = await LambdaTransport.SendAsync(client, request, cancellationToken).ConfigureAwait(false);
        return await LambdaTransport.ReadJsonAsync<LambdaUser>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Bans, or with <paramref name="banned"/> false unbans, one application user.</summary>
    public Task<LambdaUser> BanAsync(
        string subject,
        bool banned = true,
        CancellationToken cancellationToken = default) =>
        UpdateAsync(subject, new LambdaUserUpdate { Banned = banned }, cancellationToken);

    /// <summary>
    /// Issues an access token for one application user. This is how a Lambda hands a caller a
    /// credential it did not have; the token speaks for that user only, never for the project.
    /// </summary>
    /// <remarks>
    /// The project realm exposes no first-party issuance grant yet, so the current release answers
    /// every call with <see cref="LambdaApiException"/> carrying status 501 and code
    /// <c>user_token_issuance_unavailable</c>. The route, the request, and the response shape are
    /// the contract; only the realm behind it is still to come.
    /// </remarks>
    /// <param name="subject">The user's subject, a UUID.</param>
    /// <param name="scopes">One to sixteen scopes from the realm's admitted set; the realm refuses any other.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    public async Task<LambdaAccessToken> IssueTokenAsync(
        string subject,
        IReadOnlyList<string> scopes,
        CancellationToken cancellationToken = default)
    {
        if (scopes is null)
        {
            throw new ArgumentNullException(nameof(scopes));
        }
        if (scopes.Count is 0 or > 16)
        {
            throw new ArgumentException("One to sixteen scopes are required.", nameof(scopes));
        }

        foreach (var scope in scopes)
        {
            if (string.IsNullOrWhiteSpace(scope) || scope.Length > 128)
            {
                throw new ArgumentException("A bounded non-empty scope is required.", nameof(scopes));
            }
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            LambdaRoutes.UserTokens(LambdaRoutes.CanonicalSubject(subject, nameof(subject))))
        {
            Content = JsonBody(new TokenRequest { Scopes = scopes }),
        };
        var response = await LambdaTransport.SendAsync(client, request, cancellationToken).ConfigureAwait(false);
        var token = await LambdaTransport.ReadJsonAsync<LambdaAccessToken>(response, cancellationToken)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(token.AccessToken) || token.ExpiresAt == default)
        {
            throw new LambdaApiException(
                502,
                "response_invalid",
                "The Yogabase Lambda runtime API returned an unusable access token.");
        }

        return token;
    }

    private static StringContent JsonBody<T>(T value) => new(
        JsonSerializer.Serialize(value, LambdaJson.Options),
        Encoding.UTF8,
        "application/json");

    private sealed class TokenRequest
    {
        public IReadOnlyList<string> Scopes { get; set; } = Array.Empty<string>();
    }
}
