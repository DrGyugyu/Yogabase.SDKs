using System;

namespace Yogabase.Samples.ServerPathProbe;

/// <summary>
/// Sign-in context the owner supplies through environment variables. It is never read from a
/// command-line argument, never printed, and never written to disk. Only the player's own tokens are
/// read, because they are the only credential any of this probe's steps sends; a developer token or
/// any other secret-bearing variable is never touched.
/// </summary>
internal sealed class SignInContext
{
    internal const string PlayerAccessTokenVariable = "YOGABASE_PLAYER_ACCESS_TOKEN";
    internal const string PlayerRefreshTokenVariable = "YOGABASE_PLAYER_REFRESH_TOKEN";

    private SignInContext(string playerAccessToken, string? playerRefreshToken)
    {
        PlayerAccessToken = playerAccessToken;
        PlayerRefreshToken = playerRefreshToken;
    }

    /// <summary>
    /// The signed-in player's already-issued OIDC access token. This tool never performs sign-in
    /// itself (no browser, no PKCE, no password grant); the owner obtains it out of band and
    /// exports it before running the probe.
    /// </summary>
    internal string PlayerAccessToken { get; }

    /// <summary>Optional refresh token, so a run longer than the access token's lifetime keeps working.</summary>
    internal string? PlayerRefreshToken { get; }

    internal static SignInContext LoadFromEnvironment(Func<string, string?> getEnvironmentVariable)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);

        var accessToken = getEnvironmentVariable(PlayerAccessTokenVariable);
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new CliUsageException(
                $"Environment variable {PlayerAccessTokenVariable} is required and must carry a "
                + "non-blank player access token. It is never accepted as a command-line argument.");
        }

        var refreshToken = getEnvironmentVariable(PlayerRefreshTokenVariable);

        return new SignInContext(
            accessToken.Trim(),
            string.IsNullOrWhiteSpace(refreshToken) ? null : refreshToken.Trim());
    }
}
