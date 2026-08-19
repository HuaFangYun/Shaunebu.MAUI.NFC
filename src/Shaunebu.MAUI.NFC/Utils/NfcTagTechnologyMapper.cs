namespace Shaunebu.MAUI.NFC.Utils;

internal static class NfcTagTechnologyMapper
{
    internal static NfcTagTechnology FromAndroidTechName(string? techName)
    {
        if (string.IsNullOrWhiteSpace(techName))
            return NfcTagTechnology.Unknown;

        return techName switch
        {
            "android.nfc.tech.Ndef" => NfcTagTechnology.Ndef,
            "android.nfc.tech.NdefFormatable" => NfcTagTechnology.NdefFormatable,
            "android.nfc.tech.NfcA" => NfcTagTechnology.NfcA,
            "android.nfc.tech.NfcB" => NfcTagTechnology.NfcB,
            "android.nfc.tech.NfcF" => NfcTagTechnology.NfcF,
            "android.nfc.tech.NfcV" => NfcTagTechnology.NfcV,
            "android.nfc.tech.IsoDep" => NfcTagTechnology.IsoDep,
            "android.nfc.tech.MifareClassic" => NfcTagTechnology.MifareClassic,
            "android.nfc.tech.MifareUltralight" => NfcTagTechnology.MifareUltralight,
            _ => NfcTagTechnology.Unknown
        };
    }
}
