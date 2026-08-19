using Microsoft.Extensions.Logging;
using Shaunebu.MAUI.NFC.Configuration;
using Shaunebu.MAUI.NFC.Diagnostics;

namespace Shaunebu.MAUI.NFC.Utils;

internal static class NfcDiagnostics
{
    private const string LoggerName = "Shaunebu.MAUI.NFC";

    internal static ILogger? CreateLogger(NfcOptions options)
        => options.LoggerFactory?.CreateLogger(LoggerName);

    internal static void LogCapabilities(ILogger? logger, string platform, NfcCapabilities capabilities)
    {
        if (logger?.IsEnabled(LogLevel.Debug) != true)
            return;

        logger.LogDebug(
            NfcLogEventIds.AvailabilityChecked,
            "NFC availability checked on {Platform}: {Availability}. CanRead={CanRead}, CanWrite={CanWrite}, CanMakeReadOnly={CanMakeReadOnly}, Reason={PlatformReason}",
            platform,
            capabilities.Availability,
            capabilities.CanRead,
            capabilities.CanWrite,
            capabilities.CanMakeReadOnly,
            capabilities.PlatformReason);
    }

    internal static void LogTagDiscovered(ILogger? logger, string platform, ITagInfo tagInfo)
    {
        if (logger?.IsEnabled(LogLevel.Information) != true)
            return;

        var payloadLength = tagInfo.Records?.Sum(record => record.Payload?.Length ?? 0) ?? 0;
        logger.LogInformation(
            NfcLogEventIds.TagDiscovered,
            "NFC tag discovered on {Platform}. Supported={Supported}, Writable={Writable}, Formatable={Formatable}, Capacity={Capacity}, RecordCount={RecordCount}, PayloadLength={PayloadLength}, Technologies={Technologies}",
            platform,
            tagInfo.IsSupported,
            tagInfo.IsWritable,
            tagInfo.IsFormatable,
            tagInfo.Capacity,
            tagInfo.Records?.Length ?? 0,
            payloadLength,
            string.Join(",", tagInfo.GetTechnologies()));
    }

    internal static void LogEvent(ILogger? logger, LogLevel level, EventId eventId, string platform, string operation)
    {
        if (logger?.IsEnabled(level) != true)
            return;

        logger.Log(level, eventId, "NFC {Operation} on {Platform}.", operation, platform);
    }

    internal static void LogFailure(ILogger? logger, EventId eventId, string platform, string operation, Exception exception)
    {
        if (logger?.IsEnabled(LogLevel.Warning) != true)
            return;

        logger.LogWarning(
            eventId,
            exception,
            "NFC {Operation} failed on {Platform}. ErrorType={ErrorType}",
            operation,
            platform,
            exception.GetType().FullName);
    }

    internal static void LogFailure(ILogger? logger, EventId eventId, string platform, string operation, string? errorType)
    {
        if (logger?.IsEnabled(LogLevel.Warning) != true)
            return;

        logger.LogWarning(
            eventId,
            "NFC {Operation} failed on {Platform}. ErrorType={ErrorType}",
            operation,
            platform,
            string.IsNullOrWhiteSpace(errorType) ? "Unknown" : errorType);
    }

    internal static void LogSubscriberFailure(ILogger? logger, string eventName, Exception exception)
    {
        if (logger?.IsEnabled(LogLevel.Warning) != true)
            return;

        logger.LogWarning(
            NfcLogEventIds.EventSubscriberFailed,
            exception,
            "An NFC event subscriber failed. EventName={EventName}, ErrorType={ErrorType}",
            eventName,
            exception.GetType().FullName);
    }
}
