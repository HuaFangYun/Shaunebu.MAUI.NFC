using Microsoft.Extensions.Logging;

namespace Shaunebu.MAUI.NFC.Diagnostics;

/// <summary>
/// Stable logging event identifiers used by Shaunebu.MAUI.NFC.
/// </summary>
public static class NfcLogEventIds
{
    /// <summary>Availability or capability status was checked.</summary>
    public static readonly EventId AvailabilityChecked = new(1000, nameof(AvailabilityChecked));

    /// <summary>NFC listening started.</summary>
    public static readonly EventId ListeningStarted = new(1100, nameof(ListeningStarted));

    /// <summary>NFC listening stopped.</summary>
    public static readonly EventId ListeningStopped = new(1101, nameof(ListeningStopped));

    /// <summary>A tag was discovered.</summary>
    public static readonly EventId TagDiscovered = new(1200, nameof(TagDiscovered));

    /// <summary>A tag connection was opened.</summary>
    public static readonly EventId TagConnected = new(1201, nameof(TagConnected));

    /// <summary>A tag connection was closed.</summary>
    public static readonly EventId TagDisconnected = new(1202, nameof(TagDisconnected));

    /// <summary>An NDEF read completed successfully.</summary>
    public static readonly EventId NdefReadSucceeded = new(1300, nameof(NdefReadSucceeded));

    /// <summary>An NDEF read failed.</summary>
    public static readonly EventId NdefReadFailed = new(1301, nameof(NdefReadFailed));

    /// <summary>An NDEF write completed successfully.</summary>
    public static readonly EventId NdefWriteSucceeded = new(1400, nameof(NdefWriteSucceeded));

    /// <summary>An NDEF write failed.</summary>
    public static readonly EventId NdefWriteFailed = new(1401, nameof(NdefWriteFailed));

    /// <summary>A permanent read-only lock completed successfully.</summary>
    public static readonly EventId ReadOnlyLockSucceeded = new(1500, nameof(ReadOnlyLockSucceeded));

    /// <summary>A permanent read-only lock failed.</summary>
    public static readonly EventId ReadOnlyLockFailed = new(1501, nameof(ReadOnlyLockFailed));

    /// <summary>Multiple tags were detected where one tag was expected.</summary>
    public static readonly EventId MultipleTagsDetected = new(1600, nameof(MultipleTagsDetected));

    /// <summary>An NFC session was invalidated.</summary>
    public static readonly EventId SessionInvalidated = new(1700, nameof(SessionInvalidated));

    /// <summary>A consumer event subscriber threw an exception.</summary>
    public static readonly EventId EventSubscriberFailed = new(1800, nameof(EventSubscriberFailed));
}
