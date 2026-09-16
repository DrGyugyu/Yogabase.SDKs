using System;

namespace Yogabase.Samples.ServerPathProbe;

/// <summary>
/// Sign-in context the owner supplies through environment variables — never through a command-line
/// argument, never printed, and never written to disk. Only <see cref="PlayerAccessToken"/> and
/// <see cref="PlayerRefreshToken"/> are held as strings past construction; the developer-token
/// variable is checked for presence and then discarded, because none of this probe's five steps is
/// a Control-Plane call that would use it (see the "Sign-in context" section of README.md).
/// </summary>
internal sealed class SignInContext
{
    internal const string PlayerAccessTokenVariable = "YOGABASE_PLAYER_ACCESS_TOKEN";
    internal const string PlayerRefreshTokenVariable = "YOGABASE_PLAYER_REFRESH_TOKEN";
    internal const string DeveloperTokenVariable = "YOGABASE_DEVELOPER_TOKEN";

    private SignInContext(string playerAccessToken, string? playerRefreshToken, bool developerTokenProvided)
    {
        PlayerAccessToken = playerAccessToken;
        PlayerRefreshToken = playerRefreshToken;
        DeveloperTokenProvided = developerTokenProvided;
    }

    /// <summary>
    /// The signed-in player's already-issued OIDC access token. This tool never performs sign-in
    /// itself (no browser, no PKCE, no password grant) — the owner obtains it out of band and
    /// exports it before running the probe.
    /// </summary>
    internal string PlayerAccessToken { get; }

    /// <summary>Optional refresh token, so a run longer than the access token's lifetime keeps working.</summary>
    internal string? PlayerRefreshToken { get; }

    /// <summary>
    /// Whether <see cref="DeveloperTokenVariable"/> was set. Reserved for a future step (for example
    /// a Control-Plane build upload); reported so the owner can see their environment was read
    /// correctly, but this probe does not transmit it anywhere.
    /// </summary>
    internal bool DeveloperTokenProvided { get; }

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
        var developerTokenProvided = !string.IsNullOrWhiteSpace(getEnvironmentVariable(DeveloperTokenVariable));

        return new SignInContext(
            accessToken.Trim(),
            string.IsNullOrWhiteSpace(refreshToken) ? null : refreshToken.Trim(),
            developerTokenProvided);
    }
}
