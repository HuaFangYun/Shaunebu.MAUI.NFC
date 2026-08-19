namespace Shaunebu.MAUI.NFC.Tests;

public sealed class NdefPlatformSourceTests
{
    [Fact]
    public void IosTextPayloadUsesBinarySafeNSDataConstruction()
    {
        var iosSource = ReadSource("src", "Shaunebu.MAUI.NFC", "Platforms", "iOS", "NFC.iOS.cs");

        Assert.Contains("NdefRecordEncoding.TryCreateTextPayload(record.Payload, lang, out var payloadData)", iosSource);
        Assert.Contains("NSData.FromArray(payloadData)", iosSource);
        Assert.DoesNotContain("NSData.FromString(Encoding.UTF8.GetString(payloadData)", iosSource);
    }

    [Fact]
    public void IosExternalPayloadComposesDomainAndType()
    {
        var iosSource = ReadSource("src", "Shaunebu.MAUI.NFC", "Platforms", "iOS", "NFC.iOS.cs");

        Assert.Contains("NdefRecordEncoding.TryComposeExternalType(record.ExternalDomain, record.ExternalType, out var externalType)", iosSource);
        Assert.Contains("new NFCNdefPayload(NFCTypeNameFormat.NFCExternal, externalType", iosSource);
        Assert.DoesNotContain("new NFCNdefPayload(NFCTypeNameFormat.NFCExternal, record.ExternalType!", iosSource);
    }

    [Fact]
    public void CapacityChecksUseEncodedNativeMessages()
    {
        var androidSource = ReadSource("src", "Shaunebu.MAUI.NFC", "Platforms", "Android", "NFC.android.cs");
        var iosSource = ReadSource("src", "Shaunebu.MAUI.NFC", "Platforms", "iOS", "NFC.iOS.cs");
        var legacyIosSource = ReadSource("src", "Shaunebu.MAUI.NFC", "Platforms", "iOS", "NFCLegacy.iOS.cs");

        Assert.Contains("var encodedMessage = message.ToByteArray();", androidSource);
        Assert.Contains("encodedMessage.Length", androidSource);
        Assert.Contains("message.ToByteArray().Length", iosSource);
        Assert.Contains("message.ToByteArray().Length", legacyIosSource);
        Assert.DoesNotContain("capacity) < NFCUtils.GetSize(tagInfo.Records)", iosSource);
        Assert.DoesNotContain("capacity) < NFCUtils.GetSize(tagInfo.Records)", legacyIosSource);
        Assert.DoesNotContain("ndef.MaxSize < NFCUtils.GetSize(tagInfo.Records)", androidSource);
    }

    private static string ReadSource(params string[] pathParts)
        => File.ReadAllText(Path.Combine([RepositoryPaths.Root, .. pathParts]));
}
