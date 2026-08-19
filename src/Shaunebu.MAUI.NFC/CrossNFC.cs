namespace Shaunebu.MAUI.NFC;

#if ANDROID
using Android.Content;
#endif
using Shaunebu.MAUI.NFC.Configuration;

/// <summary>
/// Cross NFC
/// </summary>
public static partial class CrossNFC
{
    private static Lazy<INFC?> _implementation = new(() => CreateNFC(), LazyThreadSafetyMode.PublicationOnly);
    private static NfcOptions _options = new();
    private static bool _legacy;

    /// <summary>
    /// Gets if the plugin is supported on the current platform.
    /// </summary>
    public static bool IsSupported => _implementation.Value != null;

    /// <summary>
    /// Legacy Mode (Supporting Mifare Classic on iOS)
    /// </summary>
    internal static bool Legacy
    {
        get => _legacy;
        set => SetLegacyMode(value);
    }

    /// <summary>
    /// Gets whether the current static implementation is configured for legacy mode.
    /// </summary>
    public static bool IsLegacy => _legacy;

    /// <summary>
    /// Gets a richer capability snapshot for the current NFC service.
    /// </summary>
    /// <returns>Capability snapshot.</returns>
    public static NfcCapabilities GetCapabilities()
    {
        if (!IsSupported)
        {
            return new NfcCapabilities
            {
                Availability = NfcAvailabilityStatus.NotSupported,
                PlatformReason = "No platform-specific NFC implementation is available."
            };
        }

        return Current.GetCapabilities();
    }

    /// <summary>
    /// Current plugin implementation to use
    /// </summary>
    public static INFC Current
    {
        get
        {
            var ret = _implementation.Value;
            return ret ?? throw NotImplementedInReferenceAssembly();
        }
    }

    private static INFC? CreateNFC()
    {
        INFC? implementation;

#if ANDROID
        implementation = new NFCImplementation_Android();
#elif IOS
        ObjCRuntime.Class.ThrowOnInitFailure = false;
        implementation = Utils.NFCUtils.IsWritingSupported() && !Legacy
            ? new NFCImplementation_iOS()
            : new NFCImplementation_Before_iOS13();
#else
        implementation = null;
#endif

        if (implementation is not null)
            implementation.Options = _options;

        return implementation;
    }

    internal static Exception NotImplementedInReferenceAssembly() =>
        new NotImplementedException("This functionality is not implemented in the portable version of this assembly.  You should reference the NuGet package from your main application project in order to reference the platform-specific implementation.");

    internal static void Configure(NfcOptions options)
    {
        _options = options ?? new NfcOptions();
        SetLegacyMode(_options.LegacyMode, applyOptions: false);

        if (_implementation.Value is INFC current)
            current.Options = _options;
    }

    private static void SetLegacyMode(bool value, bool applyOptions = true)
    {
        if (_legacy == value)
        {
            if (applyOptions && _implementation.Value is INFC existing)
                existing.Options = _options;

            return;
        }

        _legacy = value;
        _options.LegacyMode = value;
        _implementation = new Lazy<INFC?>(() => CreateNFC(), LazyThreadSafetyMode.PublicationOnly);

        if (applyOptions && _implementation.Value is INFC configured)
            configured.Options = _options;
    }

#if ANDROID
    /// <summary>
    /// Converts an Android Intent to TagInfo (Android only)
    /// </summary>
    /// <param name="intent">Android NFC intent</param>
    /// <returns>TagInfo or null</returns>
    public static ITagInfo? GetTagInfo(Intent? intent)
    {
        return Current.GetTagInfo(intent);
    }
#endif
}
