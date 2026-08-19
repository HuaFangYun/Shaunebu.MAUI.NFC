using Shaunebu.MAUI.NFC.Utils;
using System.Text;

namespace Shaunebu.MAUI.NFC.Tests;

public sealed class Phase7UriTests
{
    [Theory]
    [InlineData("example.com", "example.com")]
    [InlineData("http://www.example.com", "http://www.example.com")]
    [InlineData("https://www.example.com/path", "https://www.example.com/path")]
    [InlineData("http://example.com", "http://example.com")]
    [InlineData("https://example.com", "https://example.com")]
    [InlineData("tel:+15551234567", "tel:+15551234567")]
    [InlineData("mailto:hello@example.com", "mailto:hello@example.com")]
    [InlineData("urn:nfc:ext:shaunebu.com:demo", "urn:nfc:ext:shaunebu.com:demo")]
    [InlineData("https://example.com/mañana/日本語", "https://example.com/mañana/日本語")]
    public void UriPayloadRoundTripsNfcForumPrefixes(string uri, string expected)
    {
        Assert.True(NdefRecordEncoding.TryCreateUriPayload(uri, out var payload));
        Assert.True(NdefRecordEncoding.TryParseUriPayload(payload, out var parsed));

        Assert.Equal(expected, parsed);
    }

    [Fact]
    public void UriPayloadRejectsMalformedAndPathologicalInputs()
    {
        Assert.False(NdefRecordEncoding.TryParseUriPayload(null, out _));
        Assert.False(NdefRecordEncoding.TryParseUriPayload([], out _));
        Assert.False(NdefRecordEncoding.TryParseUriPayload([0xFF, 0x61], out _));
        Assert.False(NdefRecordEncoding.TryParseUriPayload([0x04, 0xFF], out _));
        Assert.False(NdefRecordEncoding.TryCreateUriPayload(string.Empty, out _));
        Assert.False(NdefRecordEncoding.TryCreateUriPayload(new string('a', NdefRecordEncoding.MaxDecodedUriBytes + 1), out _));
    }

    [Fact]
    public void NfcUtilsUsesSafeUriMessagePath()
    {
        var uri = "https://example.com/path";
        Assert.True(NdefRecordEncoding.TryCreateUriPayload(uri, out var payload));

        var record = new NFCNdefRecord
        {
            TypeFormat = NFCNdefTypeFormat.Uri,
            Payload = Encoding.UTF8.GetBytes(uri),
            Uri = uri
        };

        Assert.Equal(uri, NFCUtils.GetMessage(record));
        Assert.True(NdefRecordEncoding.TryParseUriPayload(payload, out var parsed));
        Assert.Equal(uri, parsed);
    }
}
