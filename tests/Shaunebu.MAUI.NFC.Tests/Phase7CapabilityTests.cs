using Shaunebu.MAUI.NFC.Configuration;

namespace Shaunebu.MAUI.NFC.Tests;

public sealed class Phase7CapabilityTests
{
    [Fact]
    public void LegacyBooleansMapAvailableDisabledAndWritableStates()
    {
        var available = NfcCapabilities.FromLegacyBooleans(true, true, true);
        var disabled = NfcCapabilities.FromLegacyBooleans(true, false, true);
        var unknown = NfcCapabilities.FromLegacyBooleans(false, false, false);

        Assert.Equal(NfcAvailabilityStatus.Available, available.Availability);
        Assert.True(available.CanRead);
        Assert.True(available.CanWrite);
        Assert.True(available.CanMakeReadOnly);

        Assert.Equal(NfcAvailabilityStatus.Disabled, disabled.Availability);
        Assert.False(disabled.CanRead);
        Assert.False(disabled.CanWrite);

        Assert.Equal(NfcAvailabilityStatus.Unknown, unknown.Availability);
        Assert.False(unknown.CanRead);
    }

    [Fact]
    public void ExtensionFallsBackToLegacyBooleansForCustomImplementations()
    {
        var nfc = new FakeNfc(isAvailable: true, isEnabled: false, isWritingSupported: true);

        var capabilities = nfc.GetCapabilities();

        Assert.Equal(NfcAvailabilityStatus.Disabled, capabilities.Availability);
        Assert.False(capabilities.CanRead);
        Assert.False(capabilities.CanWrite);
        Assert.Contains("legacy boolean API", capabilities.PlatformReason);
    }

    private sealed class FakeNfc(bool isAvailable, bool isEnabled, bool isWritingSupported) : INFC
    {
        public bool IsAvailable { get; } = isAvailable;
        public bool IsEnabled { get; } = isEnabled;
        public bool IsWritingTagSupported { get; } = isWritingSupported;
        public NfcOptions Options { get; set; } = new();

        public event EventHandler? OnTagConnected;
        public event EventHandler? OnTagDisconnected;
        public event NdefMessageReceivedEventHandler? OnMessageReceived;
        public event TagDiscoveredEventHandler? OnTagDiscovered;
        public event NdefMessagePublishedEventHandler? OnMessagePublished;
        public event EventHandler? OniOSReadingSessionCancelled;
        public event OnNfcStatusChangedEventHandler? OnNfcStatusChanged;
        public event TagListeningStatusChangedEventHandler? OnTagListeningStatusChanged;

        public void EnableLegacy(bool value) => Options.LegacyMode = value;
        public void StartListening() => OnTagListeningStatusChanged?.Invoke(true);
        public void StopListening() => OnTagListeningStatusChanged?.Invoke(false);
        public void StartPublishing(bool clearMessage = false) { }
        public void StopPublishing() { }
        public void PublishMessage(ITagInfo tagInfo, bool makeReadOnly = false) => OnMessagePublished?.Invoke(tagInfo);
        public void ClearMessage(ITagInfo tagInfo) => OnMessagePublished?.Invoke(tagInfo);
        public void RaiseMessageReceived(ITagInfo tagInfo) => OnMessageReceived?.Invoke(tagInfo);

        public void TouchEvents()
        {
            OnTagConnected?.Invoke(this, EventArgs.Empty);
            OnTagDisconnected?.Invoke(this, EventArgs.Empty);
            OnTagDiscovered?.Invoke(new FakeTagInfo(), false);
            OniOSReadingSessionCancelled?.Invoke(this, EventArgs.Empty);
            OnNfcStatusChanged?.Invoke(IsEnabled);
        }
    }

    private sealed class FakeTagInfo : ITagInfo
    {
        public byte[] Identifier { get; } = [];
        public string SerialNumber { get; } = string.Empty;
        public bool IsWritable { get; set; }
        public bool IsEmpty => Records.Length == 0;
        public bool IsSupported { get; } = true;
        public bool IsFormatable { get; } = false;
        public int Capacity { get; set; }
        public NFCNdefRecord[] Records { get; set; } = [];
    }
}
