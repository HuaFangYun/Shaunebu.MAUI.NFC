using System.Text;

namespace Shaunebu.MAUI.NFC.Utils;

internal static class NdefRecordEncoding
{
    internal const string TextMimeType = "text/plain";
    internal const int MaxDecodedUriBytes = 8192;

    private const byte ShortRecordFlag = 0x10;
    private const byte Utf16StatusFlag = 0x80;
    private const byte ReservedStatusFlag = 0x40;
    private const byte LanguageCodeLengthMask = 0x3F;

    private static readonly byte[] TextRecordType = [(byte)'T'];
    private static readonly byte[] UriRecordType = [(byte)'U'];
    private static readonly byte[] Empty = [];

    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly UnicodeEncoding StrictUtf16BigEndian = new(true, false, true);
    private static readonly UnicodeEncoding StrictUtf16LittleEndian = new(false, false, true);

    private static readonly string[] UriPrefixes =
    [
        "",
        "http://www.",
        "https://www.",
        "http://",
        "https://",
        "tel:",
        "mailto:",
        "ftp://anonymous:anonymous@",
        "ftp://ftp.",
        "ftps://",
        "sftp://",
        "smb://",
        "nfs://",
        "ftp://",
        "dav://",
        "news:",
        "telnet://",
        "imap:",
        "rtsp://",
        "urn:",
        "pop:",
        "sip:",
        "sips:",
        "tftp:",
        "btspp://",
        "btl2cap://",
        "btgoep://",
        "tcpobex://",
        "irdaobex://",
        "file://",
        "urn:epc:id:",
        "urn:epc:tag:",
        "urn:epc:pat:",
        "urn:epc:raw:",
        "urn:epc:",
        "urn:nfc:",
    ];

    internal static bool IsTextRecord(NFCNdefRecord record)
        => record.TypeFormat == NFCNdefTypeFormat.WellKnown &&
           string.Equals(record.MimeType, TextMimeType, StringComparison.OrdinalIgnoreCase) &&
           string.IsNullOrWhiteSpace(record.Uri);

    internal static bool TryParseTextPayload(byte[]? payload, out string message, out string? languageCode)
    {
        message = string.Empty;
        languageCode = null;

        if (payload is null || payload.Length == 0)
            return false;

        var status = payload[0];
        if ((status & ReservedStatusFlag) != 0)
            return false;

        var languageCodeLength = status & LanguageCodeLengthMask;
        if (payload.Length < languageCodeLength + 1)
            return false;

        if (!TryDecodeAscii(payload.AsSpan(1, languageCodeLength), out languageCode))
            return false;

        var textBytes = payload.AsSpan(languageCodeLength + 1);
        if ((status & Utf16StatusFlag) == 0)
            return TryDecodeUtf8(textBytes, out message);

        return TryDecodeUtf16(textBytes, out message);
    }

    internal static bool TryCreateTextPayload(byte[]? textBytes, string? languageCode, out byte[] payload, bool useUtf16 = false)
    {
        payload = [];

        if (textBytes is null || !TryGetLanguageCodeBytes(languageCode, out var languageCodeBytes))
            return false;

        var status = (byte)(languageCodeBytes.Length | (useUtf16 ? Utf16StatusFlag : 0));
        payload = new byte[1 + languageCodeBytes.Length + textBytes.Length];
        payload[0] = status;
        languageCodeBytes.CopyTo(payload, 1);
        textBytes.CopyTo(payload, 1 + languageCodeBytes.Length);
        return true;
    }

    internal static bool TryNormalizeLanguageCode(string? languageCode, out string normalized)
    {
        normalized = string.Empty;

        if (!TryGetLanguageCodeBytes(languageCode, out _))
            return false;

        normalized = languageCode!;
        return true;
    }

    internal static bool TryComposeExternalType(string? domain, string? type, out string externalType)
    {
        externalType = string.Empty;

        if (string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(type))
            return false;

        externalType = $"{domain}:{type}";
        return true;
    }

    internal static bool TrySplitExternalType(string? externalType, out string? domain, out string? type)
    {
        domain = null;
        type = null;

        if (string.IsNullOrWhiteSpace(externalType))
            return false;

        var separatorIndex = externalType.IndexOf(':', StringComparison.Ordinal);
        if (separatorIndex <= 0 || separatorIndex == externalType.Length - 1)
            return false;

        domain = externalType[..separatorIndex];
        type = externalType[(separatorIndex + 1)..];
        return true;
    }

    internal static int GetEncodedMessageSize(IEnumerable<NFCNdefRecord?>? records, string? defaultLanguageCode = "en")
    {
        if (records is null)
            return 0;

        var size = 0;
        foreach (var record in records)
        {
            if (TryGetRecordFields(record, defaultLanguageCode, out _, out var type, out var payload))
                size += GetEncodedRecordSize(type.Length, payload.Length);
        }

        return size;
    }

    private static bool TryGetRecordFields(
        NFCNdefRecord? record,
        string? defaultLanguageCode,
        out NFCNdefTypeFormat typeNameFormat,
        out byte[] type,
        out byte[] payload)
    {
        typeNameFormat = NFCNdefTypeFormat.Empty;
        type = Empty;
        payload = Empty;

        if (record is null)
            return false;

        switch (record.TypeFormat)
        {
            case NFCNdefTypeFormat.Empty:
                return true;
            case NFCNdefTypeFormat.WellKnown:
                typeNameFormat = NFCNdefTypeFormat.WellKnown;
                type = TextRecordType;
                return TryCreateTextPayload(record.Payload, GetLanguageCode(record.LanguageCode, defaultLanguageCode), out payload);
            case NFCNdefTypeFormat.Mime:
                if (record.Payload is null || string.IsNullOrWhiteSpace(record.MimeType))
                    return false;

                typeNameFormat = NFCNdefTypeFormat.Mime;
                type = Encoding.ASCII.GetBytes(record.MimeType);
                payload = record.Payload;
                return true;
            case NFCNdefTypeFormat.Uri:
                if (!TryDecodeUtf8(record.Payload, out var uri))
                    return false;

                typeNameFormat = NFCNdefTypeFormat.WellKnown;
                type = UriRecordType;
                return TryCreateUriPayload(uri, out payload);
            case NFCNdefTypeFormat.External:
                if (record.Payload is null ||
                    !TryComposeExternalType(record.ExternalDomain, record.ExternalType, out var externalType))
                {
                    return false;
                }

                typeNameFormat = NFCNdefTypeFormat.External;
                type = Encoding.UTF8.GetBytes(externalType);
                payload = record.Payload;
                return true;
            default:
                return false;
        }
    }

    private static int GetEncodedRecordSize(int typeLength, int payloadLength)
    {
        var size = 1 + 1;
        size += payloadLength <= byte.MaxValue ? 1 : 4;
        size += typeLength;
        size += payloadLength;
        return size;
    }

    private static string? GetLanguageCode(string? recordLanguageCode, string? defaultLanguageCode)
        => string.IsNullOrWhiteSpace(recordLanguageCode) ? defaultLanguageCode : recordLanguageCode;

    private static bool TryGetLanguageCodeBytes(string? languageCode, out byte[] languageCodeBytes)
    {
        languageCodeBytes = [];

        if (string.IsNullOrWhiteSpace(languageCode))
            return false;

        if (languageCode.Any(character => character is < ' ' or > '~'))
            return false;

        languageCodeBytes = Encoding.ASCII.GetBytes(languageCode);
        return languageCodeBytes.Length <= LanguageCodeLengthMask;
    }

    private static bool IsPrintableAscii(byte value) => value is >= 0x20 and <= 0x7E;

    private static bool TryDecodeAscii(ReadOnlySpan<byte> bytes, out string value)
    {
        value = string.Empty;

        if (bytes.IsEmpty)
            return true;

        if (!bytes.ToArray().All(IsPrintableAscii))
            return false;

        value = Encoding.ASCII.GetString(bytes);
        return true;
    }

    internal static bool TryDecodeUtf8(byte[]? bytes, out string value)
    {
        value = string.Empty;
        return bytes is not null && TryDecodeUtf8(bytes.AsSpan(), out value);
    }

    private static bool TryDecodeUtf8(ReadOnlySpan<byte> bytes, out string value)
    {
        value = string.Empty;

        try
        {
            value = StrictUtf8.GetString(bytes);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    private static bool TryDecodeUtf16(ReadOnlySpan<byte> bytes, out string value)
    {
        value = string.Empty;

        if (bytes.Length == 0)
            return true;

        if (bytes.Length % 2 != 0)
            return false;

        var encoding = StrictUtf16BigEndian;
        if (bytes.Length >= 2)
        {
            if (bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                bytes = bytes[2..];
            }
            else if (bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                encoding = StrictUtf16LittleEndian;
                bytes = bytes[2..];
            }
        }

        try
        {
            value = encoding.GetString(bytes);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    internal static bool TryCreateUriPayload(string? uri, out byte[] payload)
    {
        payload = Empty;

        if (string.IsNullOrWhiteSpace(uri))
            return false;

        var uriBytes = Encoding.UTF8.GetByteCount(uri);
        if (uriBytes > MaxDecodedUriBytes)
            return false;

        var prefixIndex = 0;
        var suffix = uri;

        for (var i = 1; i < UriPrefixes.Length; i++)
        {
            if (uri.StartsWith(UriPrefixes[i], StringComparison.OrdinalIgnoreCase) &&
                UriPrefixes[i].Length > UriPrefixes[prefixIndex].Length)
            {
                prefixIndex = i;
                suffix = uri[UriPrefixes[i].Length..];
            }
        }

        payload = [(byte)prefixIndex, .. Encoding.UTF8.GetBytes(suffix)];
        return true;
    }

    internal static bool TryParseUriPayload(byte[]? payload, out string uri)
    {
        uri = string.Empty;

        if (payload is null || payload.Length < 2 || payload.Length > MaxDecodedUriBytes + 1)
            return false;

        var prefixIndex = payload[0] & 0xFF;
        if (prefixIndex >= UriPrefixes.Length)
            return false;

        if (!TryDecodeUtf8(payload.AsSpan(1), out var suffix))
            return false;

        var candidate = UriPrefixes[prefixIndex] + suffix;
        if (string.IsNullOrWhiteSpace(candidate) || !Uri.TryCreate(candidate, UriKind.RelativeOrAbsolute, out _))
            return false;

        uri = candidate;
        return true;
    }
}
