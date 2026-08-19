namespace Shaunebu.MAUI.NFC;

/// <summary>
/// Cross-platform-friendly NFC tag technology names.
/// </summary>
public enum NfcTagTechnology
{
    /// <summary>The native platform reported a technology that is not mapped by this package.</summary>
    Unknown,

    /// <summary>The tag exposes NDEF data.</summary>
    Ndef,

    /// <summary>The tag can potentially be formatted as NDEF.</summary>
    NdefFormatable,

    /// <summary>The tag reports Android NFC-A / ISO 14443 Type A technology.</summary>
    NfcA,

    /// <summary>The tag reports Android NFC-B / ISO 14443 Type B technology.</summary>
    NfcB,

    /// <summary>The tag reports Android NFC-F / FeliCa polling technology.</summary>
    NfcF,

    /// <summary>The tag reports Android NFC-V / ISO 15693 vicinity technology.</summary>
    NfcV,

    /// <summary>The tag reports ISO-DEP technology.</summary>
    IsoDep,

    /// <summary>The tag reports ISO 7816-compatible technology.</summary>
    Iso7816,

    /// <summary>The tag reports ISO 15693-compatible technology.</summary>
    Iso15693,

    /// <summary>The tag reports FeliCa-compatible technology.</summary>
    FeliCa,

    /// <summary>The tag reports a MIFARE-compatible technology.</summary>
    Mifare,

    /// <summary>The tag reports MIFARE Classic technology.</summary>
    MifareClassic,

    /// <summary>The tag reports MIFARE Ultralight technology.</summary>
    MifareUltralight
}
