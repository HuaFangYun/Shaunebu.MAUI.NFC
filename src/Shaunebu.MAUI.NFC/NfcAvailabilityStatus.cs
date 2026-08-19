namespace Shaunebu.MAUI.NFC;

/// <summary>
/// Describes the best-known NFC availability state for the current platform.
/// </summary>
public enum NfcAvailabilityStatus
{
    /// <summary>
    /// The platform cannot provide a more specific availability state.
    /// </summary>
    Unknown,

    /// <summary>
    /// NFC appears available for use.
    /// </summary>
    Available,

    /// <summary>
    /// NFC hardware is present but disabled by the user or system.
    /// </summary>
    Disabled,

    /// <summary>
    /// NFC hardware or platform support is not available.
    /// </summary>
    NotSupported,

    /// <summary>
    /// Required application configuration, permission, entitlement, or capability appears to be missing.
    /// </summary>
    ConfigurationMissing,

    /// <summary>
    /// NFC is temporarily unavailable even though the platform may support it.
    /// </summary>
    TemporarilyUnavailable
}
