namespace Shaunebu.MAUI.NFC.Tests;

public sealed class SampleSourceTests
{
    [Fact]
    public void SampleDoesNotUseTimingOrThreadPoolLifecycleWorkarounds()
    {
        var source = ReadSampleSource("MainPage.xaml.cs");

        Assert.DoesNotContain("Task.Delay", source);
        Assert.DoesNotContain("Task.Run", source);
    }

    [Fact]
    public void SampleUsesLifecycleSafeEventSubscriptionPattern()
    {
        var source = ReadSampleSource("MainPage.xaml.cs");

        Assert.Contains("protected override void OnAppearing()", source);
        Assert.Contains("protected override void OnDisappearing()", source);
        Assert.Contains("SubscribeEvents();", source);
        Assert.Contains("UnsubscribeEvents();", source);
        Assert.Contains("if (_eventsSubscribed)", source);
        Assert.Contains("if (!_eventsSubscribed)", source);
    }

    [Fact]
    public void SampleDispatchesNfcCallbacksToMainThread()
    {
        var source = ReadSampleSource("MainPage.xaml.cs");

        Assert.Contains("RunOnMainThreadAsync", source);
        Assert.Contains("MainThread.IsMainThread", source);
        Assert.Contains("MainThread.BeginInvokeOnMainThread", source);
    }

    [Fact]
    public void AndroidPublishingEnablesForegroundDispatchWithoutStartupDelay()
    {
        var source = ReadSampleSource("MainPage.xaml.cs");

        Assert.Contains("DeviceInfo.Platform != DevicePlatform.iOS", source);
        Assert.Contains("_nfc.StartListening();", source);
        Assert.Contains("_nfc.StartPublishing(clearMessage);", source);
    }

    [Fact]
    public void SampleXamlUsesCompiledBindings()
    {
        var xaml = ReadSampleSource("MainPage.xaml");

        Assert.Contains("x:DataType=\"local:MainPage\"", xaml);
        Assert.Contains("x:DataType=\"models:NdefRecordDisplay\"", xaml);
        Assert.Contains("x:DataType=\"models:SampleEventLogEntry\"", xaml);
    }

    [Fact]
    public void AndroidMainActivityOverrideUsesNullableBundle()
    {
        var source = ReadSampleSource("Platforms", "Android", "MainActivity.cs");

        Assert.Contains("OnCreate(Bundle? savedInstanceState)", source);
    }

    private static string ReadSampleSource(params string[] pathParts)
        => File.ReadAllText(Path.Combine([RepositoryPaths.Root, "samples", "Shaunebu.MAUI.NFC.Example", .. pathParts]));
}
