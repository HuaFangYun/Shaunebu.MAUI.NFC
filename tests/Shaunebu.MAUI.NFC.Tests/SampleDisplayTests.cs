using Shaunebu.MAUI.NFC.Maui.Sample.Models;
using Shaunebu.MAUI.NFC.Utils;

namespace Shaunebu.MAUI.NFC.Tests;

public sealed class SampleDisplayTests
{
    [Fact]
    public void RecordDisplayHandlesMultipleRecordsAndPayloadSummaries()
    {
        var records = new[]
        {
            new NFCNdefRecord
            {
                TypeFormat = NFCNdefTypeFormat.WellKnown,
                MimeType = "text/plain",
                Payload = NFCUtils.EncodeToByteArray("Hello"),
                LanguageCode = "en"
            },
            new NFCNdefRecord
            {
                TypeFormat = NFCNdefTypeFormat.Mime,
                MimeType = "application/json",
                Payload = Enumerable.Range(0, 32).Select(value => (byte)value).ToArray()
            }
        };

        var display = NdefRecordDisplay.FromRecords(records);

        Assert.Equal(2, display.Count);
        Assert.Equal(1, display[0].Index);
        Assert.Equal(2, display[1].Index);
        Assert.Equal("Payload: 32 bytes", display[1].PayloadSummary);
        Assert.EndsWith(" ...", display[1].HexPreview);
    }

    [Fact]
    public void EventLogEntryCreatesReadableSummary()
    {
        var entry = SampleEventLogEntry.Create("Read", "Tag read.");

        Assert.Equal("Read", entry.Kind);
        Assert.Equal("Tag read.", entry.Message);
        Assert.Contains("Read: Tag read.", entry.Summary);
    }
}
