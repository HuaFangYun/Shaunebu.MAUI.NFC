using System.Text;
using Shaunebu.MAUI.NFC;
using Shaunebu.MAUI.NFC.Utils;

namespace Shaunebu.MAUI.NFC.Tests;

public sealed class NdefTextParsingTests
{
    [Fact]
    public void MessageParsesValidUtf8TextRecord()
    {
        var record = CreateTextRecord([0x02, (byte)'e', (byte)'n', .. Encoding.UTF8.GetBytes("hello")]);

        Assert.Equal("hello", record.Message);
    }

    [Fact]
    public void MessageParsesValidUtf16BigEndianTextRecord()
    {
        var payload = new byte[] { 0x82, (byte)'e', (byte)'n' }
            .Concat(Encoding.BigEndianUnicode.GetBytes("hello"))
            .ToArray();

        Assert.Equal("hello", CreateTextRecord(payload).Message);
    }

    [Fact]
    public void MessageParsesValidUtf16LittleEndianTextRecordWithBom()
    {
        var payload = new byte[] { 0x82, (byte)'e', (byte)'n', 0xFF, 0xFE }
            .Concat(Encoding.Unicode.GetBytes("hello"))
            .ToArray();

        Assert.Equal("hello", CreateTextRecord(payload).Message);
    }

    [Fact]
    public void MessageParsesEmptyText()
    {
        Assert.Equal(string.Empty, CreateTextRecord([0x02, (byte)'e', (byte)'n']).Message);
    }

    [Fact]
    public void MessageParsesNonEnglishLanguageCode()
    {
        var record = CreateTextRecord([0x05, (byte)'p', (byte)'t', (byte)'-', (byte)'B', (byte)'R', .. Encoding.UTF8.GetBytes("olá")]);

        Assert.Equal("olá", record.Message);
        Assert.True(NdefRecordEncoding.TryParseTextPayload(record.Payload, out _, out var languageCode));
        Assert.Equal("pt-BR", languageCode);
    }

    [Fact]
    public void MessageParsesMaximumLanguageCodeLength()
    {
        var languageCode = new string('a', 63);
        var payload = new[] { (byte)languageCode.Length }
            .Concat(Encoding.ASCII.GetBytes(languageCode))
            .Concat(Encoding.UTF8.GetBytes("x"))
            .ToArray();

        Assert.Equal("x", CreateTextRecord(payload).Message);
    }

    [Theory]
    [InlineData(new byte[] { })]
    [InlineData(new byte[] { 0x40 })]
    [InlineData(new byte[] { 0x03, (byte)'e' })]
    [InlineData(new byte[] { 0x3F, (byte)'e', (byte)'n' })]
    [InlineData(new byte[] { 0x02, 0xFF, (byte)'n', (byte)'x' })]
    [InlineData(new byte[] { 0x02, (byte)'e', (byte)'n', 0xC3, 0x28 })]
    [InlineData(new byte[] { 0x82, (byte)'e', (byte)'n', 0x00 })]
    public void MalformedTextPayloadsReturnEmptyMessage(byte[] payload)
    {
        var exception = Record.Exception(() => CreateTextRecord(payload).Message);

        Assert.Null(exception);
        Assert.Equal(string.Empty, CreateTextRecord(payload).Message);
    }

    [Fact]
    public void RandomBinaryPayloadDoesNotCrash()
    {
        var payload = Enumerable.Range(0, 64).Select(value => (byte)((value * 37) % 256)).ToArray();

        var exception = Record.Exception(() => CreateTextRecord(payload).Message);

        Assert.Null(exception);
    }

    [Fact]
    public void WellKnownUriRecordIsNotParsedAsText()
    {
        var record = new NFCNdefRecord
        {
            TypeFormat = NFCNdefTypeFormat.WellKnown,
            MimeType = string.Empty,
            Uri = "https://example.com",
            Payload = [0x01, .. Encoding.UTF8.GetBytes("example.com")]
        };

        Assert.Equal("https://example.com", record.Message);
    }

    [Fact]
    public void UnknownWellKnownRecordIsNotParsedAsText()
    {
        var validTextPayload = new byte[] { 0x02, (byte)'e', (byte)'n' }
            .Concat(Encoding.UTF8.GetBytes("hello"))
            .ToArray();

        var record = new NFCNdefRecord
        {
            TypeFormat = NFCNdefTypeFormat.WellKnown,
            MimeType = string.Empty,
            Payload = validTextPayload
        };

        Assert.Equal(string.Empty, record.Message);
    }

    private static NFCNdefRecord CreateTextRecord(byte[] payload)
        => new()
        {
            TypeFormat = NFCNdefTypeFormat.WellKnown,
            MimeType = NdefRecordEncoding.TextMimeType,
            Payload = payload
        };
}
