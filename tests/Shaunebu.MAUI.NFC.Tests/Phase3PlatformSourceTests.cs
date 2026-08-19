namespace Shaunebu.MAUI.NFC.Tests;

public sealed class Phase3PlatformSourceTests
{
    [Fact]
    public void IosDetectionDoesNotUseFirstTagSelection()
    {
        var iosSource = ReadSource("src", "Shaunebu.MAUI.NFC", "Platforms", "iOS", "NFC.iOS.cs");
        var legacySource = ReadSource("src", "Shaunebu.MAUI.NFC", "Platforms", "iOS", "NFCLegacy.iOS.cs");

        Assert.DoesNotContain("tags.First()", iosSource);
        Assert.DoesNotContain("tags.First()", legacySource);
        Assert.Contains("NfcOperationGuards.SelectSingleTag(tags, out var selectedTag)", iosSource);
        Assert.Contains("NfcOperationGuards.SelectSingleTag(tags, out var selectedTag)", legacySource);
        Assert.Contains("RetrySingleTagDetection(session, selectionStatus)", iosSource);
        Assert.Contains("RetrySingleTagDetection(session, selectionStatus)", legacySource);
        Assert.Contains("_tag = null;", iosSource);
        Assert.Contains("_tag = null;", legacySource);
    }

    [Fact]
    public void IosDuplicateSessionGuardIsInternalOnly()
    {
        var iosSource = ReadSource("src", "Shaunebu.MAUI.NFC", "Platforms", "iOS", "NFC.iOS.cs");
        var legacySource = ReadSource("src", "Shaunebu.MAUI.NFC", "Platforms", "iOS", "NFCLegacy.iOS.cs");
        var interfaceSource = ReadSource("src", "Shaunebu.MAUI.NFC", "INFC.cs");

        Assert.Contains("private bool _sessionActive;", iosSource);
        Assert.Contains("if (_sessionActive)", iosSource);
        Assert.Contains("_sessionActive = false;", iosSource);
        Assert.Contains("private bool _sessionActive;", legacySource);
        Assert.Contains("if (_sessionActive)", legacySource);
        Assert.Contains("_sessionActive = false;", legacySource);
        Assert.DoesNotContain("SessionState", interfaceSource);
    }

    [Fact]
    public void IosWriteLockFailureIsObservableBeforePublishedEvent()
    {
        var iosSource = ReadSource("src", "Shaunebu.MAUI.NFC", "Platforms", "iOS", "NFC.iOS.cs");
        var legacySource = ReadSource("src", "Shaunebu.MAUI.NFC", "Platforms", "iOS", "NFCLegacy.iOS.cs");

        Assert.DoesNotContain("Console.WriteLine", iosSource);
        Assert.DoesNotContain("Console.WriteLine", legacySource);
        Assert.Contains("NfcOperationGuards.CombineWriteLockFailure(error.LocalizedDescription)", iosSource);
        Assert.Contains("NfcOperationGuards.CombineWriteLockFailure(error.LocalizedDescription)", legacySource);
        Assert.Contains("MakeTagReadOnly(session, sessionTag, tag, () => CompleteWrite(session, tagInfo, message))", iosSource);
        Assert.Contains("MakeTagReadOnly(session, tag, () => CompleteWrite(session, tagInfo, message))", legacySource);
    }

    [Fact]
    public void AndroidReadModeUsesFreshReadForMessageReceivedAndCachedReadForWriteDiscovery()
    {
        var androidSource = ReadSource("src", "Shaunebu.MAUI.NFC", "Platforms", "Android", "NFC.android.cs");

        Assert.Contains("var readMode = _isWriting ? AndroidNdefReadMode.Cached : AndroidNdefReadMode.Fresh;", androidSource);
        Assert.Contains("ReadFreshNdefMessage(ndef)", androidSource);
        Assert.Contains("ndef.Connect();", androidSource);
        Assert.Contains("message = ndef.NdefMessage;", androidSource);
        Assert.Contains(": ndef.CachedNdefMessage;", androidSource);
        Assert.Contains("if (!_isWriting)", androidSource);
        Assert.Contains("_currentTag = null;", androidSource);
    }

    [Fact]
    public void AndroidCleanupPreservesPrimaryFailure()
    {
        var androidSource = ReadSource("src", "Shaunebu.MAUI.NFC", "Platforms", "Android", "NFC.android.cs");

        Assert.Contains("CloseNdef(ndef, ref writeFailure)", androidSource);
        Assert.Contains("CloseNdef(ndef, ref readFailure)", androidSource);
        Assert.Contains("CloseNdefFormatable(ndefFormatable, ref formatFailure)", androidSource);
        Assert.Contains("NfcOperationGuards.PreservePrimaryException(primaryException", androidSource);
        Assert.DoesNotContain("Console.WriteLine(\"Cannot lock tag\")", androidSource);
    }

    private static string ReadSource(params string[] pathParts)
        => File.ReadAllText(Path.Combine([RepositoryPaths.Root, .. pathParts]));
}
