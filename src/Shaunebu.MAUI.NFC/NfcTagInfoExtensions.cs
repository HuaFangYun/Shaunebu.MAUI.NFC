namespace Shaunebu.MAUI.NFC;

/// <summary>
/// Additive metadata helpers for <see cref="ITagInfo"/>.
/// </summary>
public static class NfcTagInfoExtensions
{
    /// <summary>
    /// Gets normalized NFC technologies for a tag when the active implementation provides them.
    /// </summary>
    /// <param name="tagInfo">Tag information.</param>
    /// <returns>Detected technologies, or an empty list when unavailable.</returns>
    public static IReadOnlyList<NfcTagTechnology> GetTechnologies(this ITagInfo tagInfo)
    {
        ArgumentNullException.ThrowIfNull(tagInfo);
        return tagInfo is INfcTagTechnologyInfo metadata ? metadata.Technologies : [];
    }

    /// <summary>
    /// Gets non-sensitive platform technology names for a tag when the active implementation provides them.
    /// </summary>
    /// <param name="tagInfo">Tag information.</param>
    /// <returns>Platform technology names, or an empty list when unavailable.</returns>
    public static IReadOnlyList<string> GetPlatformTechnologies(this ITagInfo tagInfo)
    {
        ArgumentNullException.ThrowIfNull(tagInfo);
        return tagInfo is INfcTagTechnologyInfo metadata ? metadata.PlatformTechnologies : [];
    }
}
