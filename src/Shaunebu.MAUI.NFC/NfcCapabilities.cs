namespace Shaunebu.MAUI.NFC;

/// <summary>
/// Describes the current NFC capability state without exposing platform-native objects.
/// </summary>
public sealed record NfcCapabilities
{
    /// <summary>
    /// Gets the best-known availability state.
    /// </summary>
    public NfcAvailabilityStatus Availability { get; init; } = NfcAvailabilityStatus.Unknown;

    /// <summary>
    /// Gets a value indicating whether the current platform can read NFC tags according to the available runtime signal.
    /// </summary>
    public bool CanRead { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current platform can write NFC tags according to the available runtime signal.
    /// </summary>
    public bool CanWrite { get; init; }

    /// <summary>
    /// Gets a value indicating whether the current platform exposes a read-only locking path.
    /// </summary>
    public bool CanMakeReadOnly { get; init; }

    /// <summary>
    /// Gets a short, non-sensitive platform explanation for unavailable or degraded states.
    /// </summary>
    public string? PlatformReason { get; init; }

    /// <summary>
    /// Maps the original boolean availability API to a richer compatibility model.
    /// </summary>
    /// <param name="isAvailable">Value from <see cref="INFC.IsAvailable"/>.</param>
    /// <param name="isEnabled">Value from <see cref="INFC.IsEnabled"/>.</param>
    /// <param name="isWritingSupported">Value from <see cref="INFC.IsWritingTagSupported"/>.</param>
    /// <param name="platformReason">Optional non-sensitive reason.</param>
    /// <returns>A compatibility capability snapshot.</returns>
    public static NfcCapabilities FromLegacyBooleans(
        bool isAvailable,
        bool isEnabled,
        bool isWritingSupported,
        string? platformReason = null)
    {
        var availability = isAvailable switch
        {
            false => NfcAvailabilityStatus.Unknown,
            true when !isEnabled => NfcAvailabilityStatus.Disabled,
            _ => NfcAvailabilityStatus.Available
        };

        return new NfcCapabilities
        {
            Availability = availability,
            CanRead = isAvailable && isEnabled,
            CanWrite = isAvailable && isEnabled && isWritingSupported,
            CanMakeReadOnly = isAvailable && isEnabled && isWritingSupported,
            PlatformReason = platformReason
        };
    }
}
