namespace Shaunebu.MAUI.NFC.Tests;

public sealed class Phase7NdefMetadataTests
{
    [Fact]
    public void RawTypeAndIdentifierUseDefensiveCopies()
    {
        var type = new byte[] { 0x54 };
        var identifier = new byte[] { 0x01, 0x02 };
        var record = new NFCNdefRecord
        {
            Type = type,
            Identifier = identifier
        };

        type[0] = 0x55;
        identifier[0] = 0xFF;

        Assert.Equal([0x54], record.Type);
        Assert.Equal([0x01, 0x02], record.Identifier);

        var returnedType = record.Type!;
        var returnedIdentifier = record.Identifier!;
        returnedType[0] = 0x56;
        returnedIdentifier[0] = 0xEE;

        Assert.Equal([0x54], record.Type);
        Assert.Equal([0x01, 0x02], record.Identifier);
    }

    [Fact]
    public void SampleDisplayShowsRawMetadataWithoutPayloadValues()
    {
        var displays = Shaunebu.MAUI.NFC.Maui.Sample.Models.NdefRecordDisplay.FromRecords(
        [
            new NFCNdefRecord
            {
                TypeFormat = NFCNdefTypeFormat.WellKnown,
                Type = [(byte)'T'],
                Identifier = [0x01, 0x02],
                Payload = [(byte)'s', (byte)'e', (byte)'c', (byte)'r', (byte)'e', (byte)'t']
            }
        ]);

        var display = Assert.Single(displays);
        Assert.Equal("Type: 54", display.TypeSummary);
        Assert.Equal("Record ID: 01 02", display.IdentifierSummary);
        Assert.DoesNotContain("secret", display.PayloadSummary, StringComparison.OrdinalIgnoreCase);
    }
}
