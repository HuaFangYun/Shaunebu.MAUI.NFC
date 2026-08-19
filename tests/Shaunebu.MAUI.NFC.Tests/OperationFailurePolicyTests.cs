using Shaunebu.MAUI.NFC.Utils;

namespace Shaunebu.MAUI.NFC.Tests;

public sealed class OperationFailurePolicyTests
{
    [Fact]
    public void CleanupFailureIsReturnedWhenThereIsNoPrimaryFailure()
    {
        var cleanup = new InvalidOperationException("cleanup failed");

        var result = NfcOperationGuards.PreservePrimaryException(null, cleanup);

        Assert.Same(cleanup, result);
    }

    [Fact]
    public void CleanupFailureDoesNotReplacePrimaryFailure()
    {
        var primary = new InvalidOperationException("write failed");
        var cleanup = new InvalidOperationException("close failed");

        var result = NfcOperationGuards.PreservePrimaryException(primary, cleanup);

        Assert.Same(primary, result);
        Assert.Same(cleanup, result.Data["NfcCleanupException"]);
    }

    [Fact]
    public void WriteLockFailureUsesStableFallbackMessage()
    {
        Assert.Equal(
            "NFC tag was written, but making it read-only failed.",
            NfcOperationGuards.CombineWriteLockFailure(null));
    }

    [Fact]
    public void WriteLockFailurePreservesNativeDetails()
    {
        Assert.Equal(
            "NFC tag was written, but making it read-only failed. Native lock failure",
            NfcOperationGuards.CombineWriteLockFailure("Native lock failure"));
    }
}
