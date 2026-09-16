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
    public void ATokenWithOnlyTheAccessTokenSetLeavesTheOptionalFieldsAbsent()
    {
        var environment = new Dictionary<string, string>
        {
            [SignInContext.PlayerAccessTokenVariable] = "  access-token-value  ",
        };

        var context = SignInContext.LoadFromEnvironment(EnvironmentOf(environment));

        Assert.Equal("access-token-value", context.PlayerAccessToken);
        Assert.Null(context.PlayerRefreshToken);
        Assert.False(context.DeveloperTokenProvided);
    }

    [Fact]
    public void RefreshAndDeveloperTokensAreReadWhenPresent()
    {
        var environment = new Dictionary<string, string>
        {
            [SignInContext.PlayerAccessTokenVariable] = "access-token-value",
            [SignInContext.PlayerRefreshTokenVariable] = "  refresh-token-value  ",
            [SignInContext.DeveloperTokenVariable] = "ybt_something",
        };

        var context = SignInContext.LoadFromEnvironment(EnvironmentOf(environment));

        Assert.Equal("refresh-token-value", context.PlayerRefreshToken);
        Assert.True(context.DeveloperTokenProvided);
    }

    [Fact]
    public void ABlankDeveloperTokenVariableIsReportedAsAbsent()
    {
        var environment = new Dictionary<string, string>
        {
            [SignInContext.PlayerAccessTokenVariable] = "access-token-value",
            [SignInContext.DeveloperTokenVariable] = "   ",
        };

        var context = SignInContext.LoadFromEnvironment(EnvironmentOf(environment));

        Assert.False(context.DeveloperTokenProvided);
    }
}
