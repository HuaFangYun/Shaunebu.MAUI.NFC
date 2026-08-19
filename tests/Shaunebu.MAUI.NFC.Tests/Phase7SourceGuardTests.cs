namespace Shaunebu.MAUI.NFC.Tests;

public sealed class Phase7SourceGuardTests
{
    [Fact]
    public void UseNfcConfiguresStaticCurrentBeforeRegisteringSingletonAndAvoidsDuplicateRegistrations()
    {
        var source = ReadSource("src", "Shaunebu.MAUI.NFC", "Hosting", "AppHostBuilderExtensions.cs");

        Assert.Contains("CrossNFC.Configure(nfcOptions);", source);
        Assert.Contains("builder.Services.RemoveAll<INFC>();", source);
        Assert.Contains("builder.Services.AddSingleton(CrossNFC.Current);", source);
    }

    [Fact]
    public void PlatformImplementationsUseSafeEventDispatcherForPublicCallbacks()
    {
        var androidSource = ReadSource("src", "Shaunebu.MAUI.NFC", "Platforms", "Android", "NFC.android.cs");
        var iosSource = ReadSource("src", "Shaunebu.MAUI.NFC", "Platforms", "iOS", "NFC.iOS.cs");
        var legacySource = ReadSource("src", "Shaunebu.MAUI.NFC", "Platforms", "iOS", "NFCLegacy.iOS.cs");

        Assert.Contains("NfcEventDispatcher.Invoke", androidSource);
        Assert.Contains("NfcEventDispatcher.Invoke", iosSource);
        Assert.Contains("NfcEventDispatcher.Invoke", legacySource);
        Assert.DoesNotContain("OnMessageReceived?.Invoke", androidSource);
        Assert.DoesNotContain("OnMessageReceived?.Invoke", iosSource);
        Assert.DoesNotContain("OnMessageReceived?.Invoke", legacySource);
        Assert.DoesNotContain("OnMessagePublished?.Invoke", androidSource);
        Assert.DoesNotContain("OnMessagePublished?.Invoke", iosSource);
        Assert.DoesNotContain("OnMessagePublished?.Invoke", legacySource);
    }

    [Fact]
    public void ReaderModeAndAsyncApisAreExplicitlyDeferredForPhase7()
    {
        var audit = ReadSource("docs", "audit", "AUDIT.md");

        Assert.Contains("Reader Mode remains deferred", audit);
        Assert.Contains("additive async APIs remain deferred", audit);
    }

    private static string ReadSource(params string[] pathParts)
        => File.ReadAllText(Path.Combine([RepositoryPaths.Root, .. pathParts]));
}
