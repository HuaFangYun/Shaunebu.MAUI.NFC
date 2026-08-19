using Shaunebu.MAUI.NFC.Utils;

namespace Shaunebu.MAUI.NFC.Tests;

public sealed class NdefExternalTypeTests
{
    [Theory]
    [InlineData("example.com", "demo", "example.com:demo")]
    [InlineData("sub.example.org", "invoice/v1", "sub.example.org:invoice/v1")]
    [InlineData("com.example", "record-type", "com.example:record-type")]
    public void ComposesExternalDomainAndType(string domain, string type, string expected)
    {
        Assert.True(NdefRecordEncoding.TryComposeExternalType(domain, type, out var externalType));

        Assert.Equal(expected, externalType);
    }

    [Theory]
    [InlineData("", "demo")]
    [InlineData(" ", "demo")]
    [InlineData(null, "demo")]
    public void RejectsEmptyExternalDomain(string? domain, string type)
    {
        Assert.False(NdefRecordEncoding.TryComposeExternalType(domain, type, out var externalType));
        Assert.Equal(string.Empty, externalType);
    }

    [Theory]
    [InlineData("example.com", "")]
    [InlineData("example.com", " ")]
    [InlineData("example.com", null)]
    public void RejectsEmptyExternalType(string domain, string? type)
    {
        Assert.False(NdefRecordEncoding.TryComposeExternalType(domain, type, out var externalType));
        Assert.Equal(string.Empty, externalType);
    }

    [Theory]
    [InlineData("example.com:demo", "example.com", "demo")]
    [InlineData("sub.example.org:invoice/v1", "sub.example.org", "invoice/v1")]
    public void SplitsComposedExternalTypeForSemanticRoundTrip(string externalType, string expectedDomain, string expectedType)
    {
        Assert.True(NdefRecordEncoding.TrySplitExternalType(externalType, out var domain, out var type));

        Assert.Equal(expectedDomain, domain);
        Assert.Equal(expectedType, type);

        Assert.True(NdefRecordEncoding.TryComposeExternalType(domain, type, out var recomposed));
        Assert.Equal(externalType, recomposed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("example.com")]
    [InlineData(":demo")]
    [InlineData("example.com:")]
    public void RejectsMalformedExternalType(string externalType)
    {
        Assert.False(NdefRecordEncoding.TrySplitExternalType(externalType, out var domain, out var type));
        Assert.Null(domain);
        Assert.Null(type);
    }
}
