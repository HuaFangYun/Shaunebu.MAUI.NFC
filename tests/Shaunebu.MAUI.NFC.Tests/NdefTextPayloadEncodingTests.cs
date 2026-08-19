using System.Text;
using Shaunebu.MAUI.NFC.Utils;

namespace Shaunebu.MAUI.NFC.Tests;

public sealed class NdefTextPayloadEncodingTests
{
    [Fact]
    public void CreatesExactEnglishUtf8TextPayload()
    {
        Assert.True(NdefRecordEncoding.TryCreateTextPayload(Encoding.UTF8.GetBytes("hello"), "en", out var payload));

        Assert.Equal([0x02, (byte)'e', (byte)'n', (byte)'h', (byte)'e', (byte)'l', (byte)'l', (byte)'o'], payload);
    }

    [Fact]
    public void CreatesExactUtf8TextPayloadForAnotherLanguageCode()
    {
        Assert.True(NdefRecordEncoding.TryCreateTextPayload(Encoding.UTF8.GetBytes("olá"), "pt-BR", out var payload));

        Assert.Equal([0x05, (byte)'p', (byte)'t', (byte)'-', (byte)'B', (byte)'R', 0x6F, 0x6C, 0xC3, 0xA1], payload);
    }

    [Fact]
    public void CreatesExactUtf8TextPayloadForUnicodeText()
    {
        Assert.True(NdefRecordEncoding.TryCreateTextPayload(Encoding.UTF8.GetBytes("東京"), "ja", out var payload));

        Assert.Equal([0x02, (byte)'j', (byte)'a', 0xE6, 0x9D, 0xB1, 0xE4, 0xBA, 0xAC], payload);
    }

    [Fact]
    public void CreatesExactUtf8TextPayloadForEmptyText()
    {
        Assert.True(NdefRecordEncoding.TryCreateTextPayload([], "en", out var payload));

        Assert.Equal([0x02, (byte)'e', (byte)'n'], payload);
    }

    [Fact]
    public void CreatesExactUtf16TextPayloadWhenRequested()
    {
        var textBytes = Encoding.BigEndianUnicode.GetBytes("hi");

        Assert.True(NdefRecordEncoding.TryCreateTextPayload(textBytes, "en", out var payload, useUtf16: true));

        Assert.Equal([0x82, (byte)'e', (byte)'n', 0x00, 0x68, 0x00, 0x69], payload);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("é")]
    public void RejectsInvalidLanguageCodes(string languageCode)
    {
        Assert.False(NdefRecordEncoding.TryCreateTextPayload(Encoding.UTF8.GetBytes("hello"), languageCode, out var payload));
        Assert.Empty(payload);
    }

    [Fact]
    public void RejectsLanguageCodeLongerThanStatusByteCanRepresent()
    {
        var languageCode = new string('a', 64);

        Assert.False(NdefRecordEncoding.TryCreateTextPayload(Encoding.UTF8.GetBytes("hello"), languageCode, out var payload));
        Assert.Empty(payload);
    }
}
