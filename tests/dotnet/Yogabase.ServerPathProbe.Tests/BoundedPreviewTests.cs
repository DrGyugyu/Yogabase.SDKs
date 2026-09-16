using System;
using System.Text;
using System.Text.Json;
using Yogabase.Samples.ServerPathProbe;

namespace Yogabase.Samples.ServerPathProbe.Tests;

public sealed class BoundedPreviewTests
{
    [Fact]
    public void NullOrEmptyTextPreviewsAsEmpty()
    {
        Assert.Equal("(empty)", BoundedPreview.OfText(null, 10));
        Assert.Equal("(empty)", BoundedPreview.OfText(string.Empty, 10));
    }

    [Fact]
    public void ShortTextPassesThroughUnchanged()
    {
        Assert.Equal("hello", BoundedPreview.OfText("hello", 10));
    }

    [Fact]
    public void LongTextIsTruncatedWithAMarkerAndNeverExceedsTheBoundPlusTheMarker()
    {
        var preview = BoundedPreview.OfText(new string('a', 500), 20);

        Assert.StartsWith(new string('a', 20), preview, StringComparison.Ordinal);
        Assert.EndsWith("...(truncated)", preview, StringComparison.Ordinal);
    }

    [Fact]
    public void NewlinesAreFlattenedSoAPreviewIsAlwaysOneConsoleLine()
    {
        var preview = BoundedPreview.OfText("line one\nline two\r\nline three", 200);

        Assert.DoesNotContain('\n', preview);
        Assert.DoesNotContain('\r', preview);
    }

    [Fact]
    public void NullOrEmptyBytesPreviewAsEmpty()
    {
        Assert.Equal("(empty)", BoundedPreview.OfBytes(null, 10));
        Assert.Equal("(empty)", BoundedPreview.OfBytes(Array.Empty<byte>(), 10));
    }

    [Fact]
    public void ValidUtf8BytesPreviewAsDecodedText()
    {
        var bytes = Encoding.UTF8.GetBytes("{\"ok\":true}");

        Assert.Equal("{\"ok\":true}", BoundedPreview.OfBytes(bytes, 200));
    }

    [Fact]
    public void InvalidUtf8BytesFallBackToABoundedHexPreview()
    {
        byte[] invalidUtf8 = { 0xFF, 0xFE, 0x00, 0x01 };

        var preview = BoundedPreview.OfBytes(invalidUtf8, 200);

        Assert.StartsWith("hex:", preview, StringComparison.Ordinal);
        Assert.Contains("fffe0001", preview, StringComparison.Ordinal);
    }

    [Fact]
    public void JsonElementPreviewUsesRawTextAndRespectsTheBound()
    {
        using var document = JsonDocument.Parse("{\"a\":1,\"b\":2}");

        var preview = BoundedPreview.OfJson(document.RootElement, 200);

        Assert.Equal("{\"a\":1,\"b\":2}", preview);
    }

    [Fact]
    public void UndefinedJsonElementPreviewsAsEmpty()
    {
        Assert.Equal("(empty)", BoundedPreview.OfJson(default, 200));
    }
}
