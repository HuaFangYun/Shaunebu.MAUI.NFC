using System.Text;
using Shaunebu.MAUI.NFC;
using Shaunebu.MAUI.NFC.Utils;

namespace Shaunebu.MAUI.NFC.Tests;

public sealed class NdefEncodedSizeTests
{
    [Fact]
    public void SingleTextRecordSizeIncludesHeaderTypeAndTextStatusBytes()
    {
        var record = TextRecord("hello");

        Assert.Equal(12, NFCUtils.GetSize([record]));
        Assert.True(NFCUtils.GetSize([record]) > record.Payload!.Length);
    }

    [Fact]
    public void UriRecordSizeIncludesWellKnownTypeAndCompressedPrefix()
    {
        var record = new NFCNdefRecord
        {
            TypeFormat = NFCNdefTypeFormat.Uri,
            Payload = Encoding.UTF8.GetBytes("https://www.example.com/a")
        };

        Assert.Equal(18, NFCUtils.GetSize([record]));
    }

    [Fact]
    public void MimeRecordSizeIncludesMimeType()
    {
        var record = new NFCNdefRecord
        {
            TypeFormat = NFCNdefTypeFormat.Mime,
            MimeType = "application/json",
            Payload = [0x7B, 0x7D]
        };

        Assert.Equal(21, NFCUtils.GetSize([record]));
    }

    [Fact]
    public void ExternalRecordSizeIncludesComposedDomainAndType()
    {
        var record = ExternalRecord("example.com", "demo", [0x01, 0x02]);

        Assert.Equal(21, NFCUtils.GetSize([record]));
    }

    [Fact]
    public void MultipleRecordSizeSumsEncodedRecords()
    {
        Assert.Equal(33, NFCUtils.GetSize([TextRecord("hello"), ExternalRecord("example.com", "demo", [0x01, 0x02])]));
    }

    [Fact]
    public void EmptyPayloadStillIncludesNdefRecordOverhead()
    {
        var record = new NFCNdefRecord
        {
            TypeFormat = NFCNdefTypeFormat.Mime,
            MimeType = "text/plain",
            Payload = []
        };

        Assert.Equal(13, NFCUtils.GetSize([record]));
    }

    [Fact]
    public void PayloadLengthBoundaryUsesActualShortRecordLimit()
    {
        var shortRecord = new NFCNdefRecord
        {
            TypeFormat = NFCNdefTypeFormat.Mime,
            MimeType = "text/plain",
            Payload = new byte[255]
        };

        var longRecord = new NFCNdefRecord
        {
            TypeFormat = NFCNdefTypeFormat.Mime,
            MimeType = "text/plain",
            Payload = new byte[256]
        };

        Assert.Equal(268, NFCUtils.GetSize([shortRecord]));
        Assert.Equal(272, NFCUtils.GetSize([longRecord]));
    }

    [Fact]
    public void NullAndInvalidRecordsHaveDeterministicSize()
    {
        Assert.Equal(0, NdefRecordEncoding.GetEncodedMessageSize(null));
        Assert.Equal(0, NFCUtils.GetSize(null!));
        Assert.Equal(0, NFCUtils.GetSize([new NFCNdefRecord { TypeFormat = NFCNdefTypeFormat.External, Payload = [0x01] }]));
    }

    private static NFCNdefRecord TextRecord(string text)
        => new()
        {
            TypeFormat = NFCNdefTypeFormat.WellKnown,
            Payload = Encoding.UTF8.GetBytes(text),
            LanguageCode = "en"
        };

    private static NFCNdefRecord ExternalRecord(string domain, string type, byte[] payload)
        => new()
        {
            TypeFormat = NFCNdefTypeFormat.External,
            ExternalDomain = domain,
            ExternalType = type,
            Payload = payload
        };
}
