using Microsoft.Extensions.Logging;

namespace Shaunebu.MAUI.NFC.Configuration;

public sealed class NfcOptions
{
    /// <summary>
    /// Gets or sets legacy mode for iOS
    /// </summary>
    public bool LegacyMode { get; set; } = false;

    /// <summary>
    /// Gets or sets nfc configuration
    /// </summary>
    public NfcConfiguration Configuration { get; set; } = NfcConfiguration.GetDefaultConfiguration();

    /// <summary>
    /// Gets or sets how public NFC event subscribers are invoked.
    /// </summary>
    public NfcEventDispatchMode EventDispatchMode { get; set; } = NfcEventDispatchMode.Native;

    /// <summary>
    /// Gets or sets an optional logger factory for structured diagnostics.
    /// </summary>
    public ILoggerFactory? LoggerFactory { get; set; }
}
