namespace Shaunebu.MAUI.NFC;

/// <summary>
/// Optional tag technology metadata exposed by implementations that can provide it.
/// </summary>
public interface INfcTagTechnologyInfo
{
    /// <summary>
    /// Gets normalized NFC technologies detected for this tag.
    /// </summary>
    IReadOnlyList<NfcTagTechnology> Technologies { get; }

    /// <summary>
    /// Gets non-sensitive platform technology names when available.
    /// </summary>
    IReadOnlyList<string> PlatformTechnologies { get; }
}
