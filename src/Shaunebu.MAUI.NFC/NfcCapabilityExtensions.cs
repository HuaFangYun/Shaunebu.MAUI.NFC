namespace Shaunebu.MAUI.NFC;

/// <summary>
/// Additive capability helpers for the existing <see cref="INFC"/> API.
/// </summary>
public static class NfcCapabilityExtensions
{
    /// <summary>
    /// Gets a richer capability snapshot for the current NFC service.
    /// </summary>
    /// <param name="nfc">NFC service.</param>
    /// <returns>Capability snapshot.</returns>
    public static NfcCapabilities GetCapabilities(this INFC nfc)
    {
        ArgumentNullException.ThrowIfNull(nfc);

        if (nfc is INfcCapabilityProvider provider)
            return provider.GetCapabilities();

        return NfcCapabilities.FromLegacyBooleans(
            nfc.IsAvailable,
            nfc.IsEnabled,
            nfc.IsWritingTagSupported,
            "Capabilities were mapped from the legacy boolean API.");
    }
}

internal interface INfcCapabilityProvider
{
    NfcCapabilities GetCapabilities();
}
