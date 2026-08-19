using Shaunebu.MAUI.NFC.Utils;

namespace Shaunebu.MAUI.NFC.Tests;

public sealed class IosTagSelectionTests
{
    [Fact]
    public void NullTagCollectionIsHandledAsNoTags()
    {
        var status = NfcOperationGuards.SelectSingleTag<object>(null, out var tag);

        Assert.Equal(NfcTagSelectionStatus.NoTags, status);
        Assert.Null(tag);
    }

    [Fact]
    public void EmptyTagCollectionIsHandledAsNoTags()
    {
        var status = NfcOperationGuards.SelectSingleTag<object>([], out var tag);

        Assert.Equal(NfcTagSelectionStatus.NoTags, status);
        Assert.Null(tag);
    }

    [Fact]
    public void ExactlyOneTagIsSelected()
    {
        var expected = new object();

        var status = NfcOperationGuards.SelectSingleTag([expected], out var tag);

        Assert.Equal(NfcTagSelectionStatus.SingleTag, status);
        Assert.Same(expected, tag);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    public void MultipleTagsAreRejectedWithoutSelectingAnArbitraryTag(int count)
    {
        var tags = Enumerable.Range(0, count).Select(_ => new object()).ToArray();

        var status = NfcOperationGuards.SelectSingleTag(tags, out var tag);

        Assert.Equal(NfcTagSelectionStatus.MultipleTags, status);
        Assert.Null(tag);
    }

    [Fact]
    public void MultipleTagMessageIsDeterministic()
    {
        Assert.Equal(
            "More than one NFC tag detected. Please present only one tag.",
            NfcOperationGuards.MultipleTagsDetectedMessage);
    }
}
