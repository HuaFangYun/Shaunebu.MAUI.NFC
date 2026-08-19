using Shaunebu.MAUI.NFC.Maui.Sample.Models;
using Shaunebu.MAUI.NFC.Maui.Sample.Services;
using System.Text;

namespace Shaunebu.MAUI.NFC.Tests;

public sealed class SampleNdefRecordFactoryTests
{
    [Fact]
    public void TextRecordRequiresPayloadAndLanguageCode()
    {
        var created = SampleNdefRecordFactory.TryCreate(
            WriteRecordKind.Text,
            "Hello NFC",
            "en",
            null,
            null,
            null,
            null,
            null,
            null,
            out var record,
            out var error);

        Assert.True(created, error);
        Assert.NotNull(record);
        Assert.Equal(NFCNdefTypeFormat.WellKnown, record.TypeFormat);
        Assert.Equal("text/plain", record.MimeType);
        Assert.Equal("en", record.LanguageCode);
        Assert.Equal("Hello NFC", Encoding.UTF8.GetString(record.Payload!));
    }

    [Fact]
    public void UriRecordRejectsUnsafeSchemes()
    {
        var created = SampleNdefRecordFactory.TryCreate(
            WriteRecordKind.Uri,
            null,
            null,
            "javascript:alert(1)",
            null,
            null,
            null,
            null,
            null,
            out var record,
            out var error);

        Assert.False(created);
        Assert.Null(record);
        Assert.Contains("absolute http, https, mailto, or tel URI", error);
    }

    [Fact]
    public void UriRecordAcceptsHttpsUri()
    {
        var created = SampleNdefRecordFactory.TryCreate(
            WriteRecordKind.Uri,
            null,
            null,
            "https://github.com/Shaunebu/Shaunebu.MAUI.NFC",
            null,
            null,
            null,
            null,
            null,
            out var record,
            out var error);

        Assert.True(created, error);
        Assert.NotNull(record);
        Assert.Equal(NFCNdefTypeFormat.Uri, record.TypeFormat);
    }

    [Fact]
    public void MimeRecordRequiresValidMimeType()
    {
        var created = SampleNdefRecordFactory.TryCreate(
            WriteRecordKind.Mime,
            null,
            null,
            null,
            "not a mime type",
            "payload",
            null,
            null,
            null,
            out var record,
            out var error);

        Assert.False(created);
        Assert.Null(record);
        Assert.Contains("MIME type", error);
    }

    [Fact]
    public void ExternalRecordUsesDomainTypeFields()
    {
        var created = SampleNdefRecordFactory.TryCreate(
            WriteRecordKind.External,
            null,
            null,
            null,
            null,
            null,
            "example.com",
            "asset",
            "asset-123",
            out var record,
            out var error);

        Assert.True(created, error);
        Assert.NotNull(record);
        Assert.Equal(NFCNdefTypeFormat.External, record.TypeFormat);
        Assert.Equal("example.com", record.ExternalDomain);
        Assert.Equal("asset", record.ExternalType);
    }
}
