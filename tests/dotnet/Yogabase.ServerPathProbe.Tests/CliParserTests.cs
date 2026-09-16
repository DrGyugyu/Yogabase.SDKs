using System;
using System.Collections.Generic;
using Yogabase.Samples.ServerPathProbe;

namespace Yogabase.Samples.ServerPathProbe.Tests;

public sealed class CliParserTests
{
    private static readonly string[] RequiredArguments =
    {
        "--gateway-url", "https://api.example.com",
        "--publishable-key", "sy_pub_example",
        "--service", "adaptive-sync-demo",
        "--function-id", "bp_claim_reward",
    };

    private static List<string> WithExtra(params string[] extra)
    {
        var arguments = new List<string>(RequiredArguments);
        arguments.AddRange(extra);
        return arguments;
    }

    [Fact]
    public void ParsesTheRequiredArgumentsWithDocumentedDefaults()
    {
        var options = CliParser.Parse(RequiredArguments);

        Assert.NotNull(options);
        Assert.Equal(new Uri("https://api.example.com"), options!.GatewayUrl);
        Assert.Equal("sy_pub_example", options.PublishableKey);
        Assert.Equal("adaptive-sync-demo", options.Service);
        Assert.Equal("bp_claim_reward", options.FunctionId);
        Assert.Equal("1", options.FunctionVersion);
        Assert.Equal("{}", options.PayloadJson);
        Assert.Equal("game", options.PortName);
        Assert.Null(options.AddressOverride);
        Assert.Equal(500, options.UdpTimeoutMilliseconds);
        Assert.Null(options.RealtimeTopic);
        Assert.Equal(5, options.RealtimeSeconds);
        Assert.False(options.AllowInsecureLoopback);
        Assert.False(options.StopServerAfter);
        Assert.Equal(60, options.TimeoutSeconds);
    }

    [Theory]
    [InlineData("-h")]
    [InlineData("--help")]
    public void HelpFlagReturnsNullRegardlessOfOtherArguments(string helpFlag)
    {
        Assert.Null(CliParser.Parse(new[] { helpFlag }));
        Assert.Null(CliParser.Parse(WithExtra(helpFlag).ToArray()));
    }

    [Fact]
    public void MissingRequiredArgumentIsRefused()
    {
        var arguments = new[]
        {
            "--gateway-url", "https://api.example.com",
            "--publishable-key", "sy_pub_example",
            "--service", "adaptive-sync-demo",
            // --function-id omitted
        };

        var exception = Assert.Throws<CliUsageException>(() => CliParser.Parse(arguments));
        Assert.Contains("--function-id", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GatewayUrlMustBeAnAbsoluteUri()
    {
        var arguments = new[]
        {
            "--gateway-url", "not-a-url",
            "--publishable-key", "sy_pub_example",
            "--service", "adaptive-sync-demo",
            "--function-id", "bp_claim_reward",
        };

        Assert.Throws<CliUsageException>(() => CliParser.Parse(arguments));
    }

    [Fact]
    public void PublishableKeyMustCarryItsPrefix()
    {
        var arguments = new[]
        {
            "--gateway-url", "https://api.example.com",
            "--publishable-key", "not-a-publishable-key",
            "--service", "adaptive-sync-demo",
            "--function-id", "bp_claim_reward",
        };

        var exception = Assert.Throws<CliUsageException>(() => CliParser.Parse(arguments));
        Assert.Contains("sy_pub_", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnrecognizedArgumentIsRefused()
    {
        Assert.Throws<CliUsageException>(() => CliParser.Parse(WithExtra("--not-a-real-flag").ToArray()));
    }

    [Fact]
    public void AFlagThatNeedsAValueButHasNoneIsRefused()
    {
        Assert.Throws<CliUsageException>(() => CliParser.Parse(WithExtra("--port-name").ToArray()));
    }

    [Theory]
    [InlineData("--udp-timeout-ms", "-1")]
    [InlineData("--udp-timeout-ms", "10001")]
    [InlineData("--realtime-seconds", "0")]
    [InlineData("--realtime-seconds", "301")]
    [InlineData("--timeout-seconds", "4")]
    [InlineData("--timeout-seconds", "601")]
    [InlineData("--timeout-seconds", "not-a-number")]
    public void OutOfRangeOrNonNumericIntegerOptionsAreRefused(string flag, string value)
    {
        Assert.Throws<CliUsageException>(() => CliParser.Parse(WithExtra(flag, value).ToArray()));
    }

    [Fact]
    public void ARealtimeWindowThatWouldOutliveTheOverallTimeoutIsRefusedUpFront()
    {
        var arguments = WithExtra(
            "--realtime-topic", "inventory.updated",
            "--realtime-seconds", "50",
            "--timeout-seconds", "30");

        var exception = Assert.Throws<CliUsageException>(() => CliParser.Parse(arguments.ToArray()));
        Assert.Contains("--timeout-seconds", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ARealtimeWindowStrictlyBelowTheOverallTimeoutIsAccepted()
    {
        var arguments = WithExtra(
            "--realtime-topic", "inventory.updated",
            "--realtime-seconds", "10",
            "--timeout-seconds", "30");

        var options = CliParser.Parse(arguments.ToArray());

        Assert.NotNull(options);
        Assert.Equal("inventory.updated", options!.RealtimeTopic);
        Assert.Equal(10, options.RealtimeSeconds);
        Assert.Equal(30, options.TimeoutSeconds);
    }

    [Fact]
    public void AddressOverrideAndBooleanFlagsAreCapturedWhenGiven()
    {
        var arguments = WithExtra(
            "--address-override", "127.0.0.1",
            "--allow-insecure-loopback",
            "--stop-server",
            "--udp-message", "probe");

        var options = CliParser.Parse(arguments.ToArray());

        Assert.NotNull(options);
        Assert.Equal("127.0.0.1", options!.AddressOverride);
        Assert.True(options.AllowInsecureLoopback);
        Assert.True(options.StopServerAfter);
        Assert.Equal("probe", options.UdpMessage);
    }
}
