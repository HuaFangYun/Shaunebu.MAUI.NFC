namespace Shaunebu.MAUI.NFC.Utils;

internal enum NfcTagSelectionStatus
{
    NoTags,
    SingleTag,
    MultipleTags
}

internal static class NfcOperationGuards
{
    internal const string MultipleTagsDetectedMessage = "More than one NFC tag detected. Please present only one tag.";

    internal const string WriteLockFailedMessage = "NFC tag was written, but making it read-only failed.";

    internal static NfcTagSelectionStatus SelectSingleTag<T>(IReadOnlyList<T>? tags, out T? tag)
        where T : class
    {
        tag = null;

        if (tags is null || tags.Count == 0)
            return NfcTagSelectionStatus.NoTags;

        if (tags.Count > 1)
            return NfcTagSelectionStatus.MultipleTags;

        tag = tags[0];
        return NfcTagSelectionStatus.SingleTag;
    }

    internal static Exception PreservePrimaryException(Exception? primaryException, Exception cleanupException)
    {
        if (primaryException is null)
            return cleanupException;

        primaryException.Data["NfcCleanupException"] = cleanupException;
        return primaryException;
    }

    internal static string CombineWriteLockFailure(string? nativeMessage)
        => string.IsNullOrWhiteSpace(nativeMessage)
            ? WriteLockFailedMessage
            : $"{WriteLockFailedMessage} {nativeMessage}";
}
