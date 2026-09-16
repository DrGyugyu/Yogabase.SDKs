using System;
using System.Collections.Generic;
using Yogabase.Samples.ServerPathProbe;

namespace Yogabase.Samples.ServerPathProbe.Tests;

public sealed class SignInContextTests
{
    private static Func<string, string?> EnvironmentOf(Dictionary<string, string> values) =>
        name => values.TryGetValue(name, out var value) ? value : null;

    [Fact]
    public void MissingPlayerAccessTokenIsRefused()
    {
        var exception = Assert.Throws<CliUsageException>(() =>
            SignInContext.LoadFromEnvironment(EnvironmentOf(new Dictionary<string, string>())));

        Assert.Contains(SignInContext.PlayerAccessTokenVariable, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankPlayerAccessTokenIsRefused(string blank)
    {
        var environment = new Dictionary<string, string> { [SignInContext.PlayerAccessTokenVariable] = blank };

        Assert.Throws<CliUsageException>(() => SignInContext.LoadFromEnvironment(EnvironmentOf(environment)));
    }

    [Fact]
    public void ATokenWithOnlyTheAccessTokenSetLeavesTheRefreshTokenAbsent()
    {
        var environment = new Dictionary<string, string>
        {
            [SignInContext.PlayerAccessTokenVariable] = "  access-token-value  ",
        };

        var context = SignInContext.LoadFromEnvironment(EnvironmentOf(environment));

        Assert.Equal("access-token-value", context.PlayerAccessToken);
        Assert.Null(context.PlayerRefreshToken);
    }

    [Fact]
    public void RefreshTokenIsReadWhenPresent()
    {
        var environment = new Dictionary<string, string>
        {
            [SignInContext.PlayerAccessTokenVariable] = "access-token-value",
            [SignInContext.PlayerRefreshTokenVariable] = "  refresh-token-value  ",
        };

        var context = SignInContext.LoadFromEnvironment(EnvironmentOf(environment));

        Assert.Equal("refresh-token-value", context.PlayerRefreshToken);
    }

    [Fact]
    public void ABlankRefreshTokenIsTreatedAsAbsent()
    {
        var environment = new Dictionary<string, string>
        {
            [SignInContext.PlayerAccessTokenVariable] = "access-token-value",
            [SignInContext.PlayerRefreshTokenVariable] = "   ",
        };

        var context = SignInContext.LoadFromEnvironment(EnvironmentOf(environment));

        Assert.Null(context.PlayerRefreshToken);
    }

    // The probe holds only the credential its steps actually send. It never reads a developer token
    // or any other secret-bearing variable, so there is nothing else to leak.
    [Fact]
    public void OnlyThePlayerTokenVariablesAreRead()
    {
        var requested = new List<string>();
        var environment = new Dictionary<string, string>
        {
            [SignInContext.PlayerAccessTokenVariable] = "access-token-value",
            ["YOGABASE_DEVELOPER_TOKEN"] = "ybt_should_never_be_read",
        };

        SignInContext.LoadFromEnvironment(name =>
        {
            requested.Add(name);
            return environment.TryGetValue(name, out var value) ? value : null;
        });

        Assert.Equal(
            [SignInContext.PlayerAccessTokenVariable, SignInContext.PlayerRefreshTokenVariable],
            requested);
    }
}
