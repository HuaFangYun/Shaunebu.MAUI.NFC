using Microsoft.Extensions.Logging;
using Shaunebu.MAUI.NFC.Diagnostics;
using Shaunebu.MAUI.NFC.Utils;

namespace Shaunebu.MAUI.NFC.Tests;

public sealed class Phase7TechnologyAndDiagnosticsTests
{
    [Fact]
    public void AndroidTechnologyNamesMapToNormalizedValues()
    {
        Assert.Equal(NfcTagTechnology.Ndef, NfcTagTechnologyMapper.FromAndroidTechName("android.nfc.tech.Ndef"));
        Assert.Equal(NfcTagTechnology.NfcA, NfcTagTechnologyMapper.FromAndroidTechName("android.nfc.tech.NfcA"));
        Assert.Equal(NfcTagTechnology.IsoDep, NfcTagTechnologyMapper.FromAndroidTechName("android.nfc.tech.IsoDep"));
        Assert.Equal(NfcTagTechnology.MifareUltralight, NfcTagTechnologyMapper.FromAndroidTechName("android.nfc.tech.MifareUltralight"));
        Assert.Equal(NfcTagTechnology.Unknown, NfcTagTechnologyMapper.FromAndroidTechName("android.nfc.tech.FutureThing"));
    }

    [Fact]
    public void TagInfoExtensionsReturnOptionalMetadataSafely()
    {
        var tag = new MetadataTagInfo();

        Assert.Equal([NfcTagTechnology.Ndef, NfcTagTechnology.NfcA], tag.GetTechnologies());
        Assert.Equal(["android.nfc.tech.Ndef"], tag.GetPlatformTechnologies());
        Assert.Empty(new PlainTagInfo().GetTechnologies());
    }

    [Fact]
    public void DiagnosticsRedactPayloadAndIdentifiers()
    {
        var logger = new CapturingLogger { Enabled = true };
        var tag = new MetadataTagInfo
        {
            SerialNumber = "SECRET-SERIAL",
            Identifier = [(byte)'I', (byte)'D'],
            Records =
            [
                new NFCNdefRecord
                {
                    Payload = "super-secret-payload"u8.ToArray(),
                    Uri = "https://secret.example"
                }
            ]
        };

        NfcDiagnostics.LogTagDiscovered(logger, "Test", tag);

        var message = Assert.Single(logger.Messages);
        Assert.Contains(NfcLogEventIds.TagDiscovered.Name!, message);
        Assert.Contains("PayloadLength", message);
        Assert.DoesNotContain("super-secret-payload", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SECRET-SERIAL", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret.example", message, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class MetadataTagInfo : ITagInfo, INfcTagTechnologyInfo
    {
        public byte[] Identifier { get; set; } = [];
        public string SerialNumber { get; set; } = string.Empty;
        public bool IsWritable { get; set; } = true;
        public bool IsEmpty => Records.Length == 0;
        public bool IsSupported { get; } = true;
        public bool IsFormatable { get; } = false;
        public int Capacity { get; set; } = 144;
        public NFCNdefRecord[] Records { get; set; } = [];
        public IReadOnlyList<NfcTagTechnology> Technologies { get; } = [NfcTagTechnology.Ndef, NfcTagTechnology.NfcA];
        public IReadOnlyList<string> PlatformTechnologies { get; } = ["android.nfc.tech.Ndef"];
    }

    private sealed class PlainTagInfo : ITagInfo
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

    private sealed class CapturingLogger : ILogger
    {
        public bool Enabled { get; init; }
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel) => Enabled;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add($"{eventId.Name}: {formatter(state, exception)}");
        }
    }
}
