using System.Text.RegularExpressions;

namespace Shaunebu.MAUI.NFC.Tests;

public sealed class AndroidNullabilityContractTests
{
    [Fact]
    public void AndroidGetTagInfoPublicContractsDeclareNullableIntentAndResult()
    {
        var interfaceSource = ReadSource("src", "Shaunebu.MAUI.NFC", "INFC.cs");
        var crossNfcSource = ReadSource("src", "Shaunebu.MAUI.NFC", "CrossNFC.cs");
        var androidSource = ReadSource("src", "Shaunebu.MAUI.NFC", "Platforms", "Android", "NFC.android.cs");

        Assert.Contains("ITagInfo? GetTagInfo(Intent? intent);", interfaceSource);
        Assert.Contains("public static ITagInfo? GetTagInfo(Intent? intent)", crossNfcSource);
        Assert.Contains("public ITagInfo? GetTagInfo(Intent? intent)", androidSource);
    }

    [Fact]
    public void AndroidGetTagInfoStillPreservesDocumentedNullCases()
    {
        var androidSource = ReadSource("src", "Shaunebu.MAUI.NFC", "Platforms", "Android", "NFC.android.cs");
        var publicMethod = Regex.Match(
            androidSource,
            @"public ITagInfo\? GetTagInfo\(Intent\? intent\)(?<body>[\s\S]*?\n    \})");

        Assert.True(publicMethod.Success);
        Assert.Contains("if (intent == null)", publicMethod.Value);
        Assert.Contains("return null;", publicMethod.Value);
        Assert.Contains("intent.Action != NfcAdapter.ActionNdefDiscovered", publicMethod.Value);
        Assert.Contains("intent.Action != NfcAdapter.ActionTagDiscovered", publicMethod.Value);
    }

    private static string ReadSource(params string[] pathParts)
        => File.ReadAllText(Path.Combine([RepositoryPaths.Root, .. pathParts]));
}
