using Shaunebu.MAUI.NFC.Maui.Sample.Models;
using Shaunebu.MAUI.NFC.Utils;

namespace Shaunebu.MAUI.NFC.Maui.Sample.Services;

public static class SampleNdefRecordFactory
{
    public static bool TryCreate(
        WriteRecordKind kind,
        string? text,
        string? languageCode,
        string? uri,
        string? mimeType,
        string? mimePayload,
        string? externalDomain,
        string? externalType,
        string? externalPayload,
        out NFCNdefRecord? record,
        out string error)
    {
        record = null;
        error = string.Empty;

        switch (kind)
        {
            case WriteRecordKind.Text:
                return TryCreateText(text, languageCode, out record, out error);
            case WriteRecordKind.Uri:
                return TryCreateUri(uri, out record, out error);
            case WriteRecordKind.Mime:
                return TryCreateMime(mimeType, mimePayload, out record, out error);
            case WriteRecordKind.External:
                return TryCreateExternal(externalDomain, externalType, externalPayload, out record, out error);
            default:
                error = "Select a supported NDEF record type.";
                return false;
        }
    }

    public static bool IsSafeUri(string? value)
        => TryCreateUriValue(value, out _);

    private static bool TryCreateText(string? text, string? languageCode, out NFCNdefRecord? record, out string error)
    {
        record = null;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Enter text to write.";
            return false;
        }

        if (!IsValidLanguageCode(languageCode))
        {
            error = "Enter an ASCII language code such as en or fr.";
            return false;
        }

        record = new NFCNdefRecord
        {
            TypeFormat = NFCNdefTypeFormat.WellKnown,
            MimeType = "text/plain",
            Payload = NFCUtils.EncodeToByteArray(text.Trim()),
            LanguageCode = languageCode!.Trim()
        };

        return true;
    }

    private static bool TryCreateUri(string? uri, out NFCNdefRecord? record, out string error)
    {
        record = null;
        error = string.Empty;

        if (!TryCreateUriValue(uri, out var value))
        {
            error = "Enter an absolute http, https, mailto, or tel URI.";
            return false;
        }

        record = new NFCNdefRecord
        {
            TypeFormat = NFCNdefTypeFormat.Uri,
            Payload = NFCUtils.EncodeToByteArray(value)
        };

        return true;
    }

    private static bool TryCreateMime(string? mimeType, string? payload, out NFCNdefRecord? record, out string error)
    {
        record = null;
        error = string.Empty;

        if (!IsValidMimeType(mimeType))
        {
            error = "Enter a MIME type such as text/plain or application/json.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            error = "Enter MIME payload text.";
            return false;
        }

        record = new NFCNdefRecord
        {
            TypeFormat = NFCNdefTypeFormat.Mime,
            MimeType = mimeType!.Trim(),
            Payload = NFCUtils.EncodeToByteArray(payload)
        };

        return true;
    }

    private static bool TryCreateExternal(
        string? domain,
        string? type,
        string? payload,
        out NFCNdefRecord? record,
        out string error)
    {
        record = null;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(domain) || domain.Contains(':'))
        {
            error = "Enter an external domain without a colon, such as example.com.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(type) || type.Contains(':'))
        {
            error = "Enter an external type without a colon, such as asset.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            error = "Enter external payload text.";
            return false;
        }

        record = new NFCNdefRecord
        {
            TypeFormat = NFCNdefTypeFormat.External,
            ExternalDomain = domain.Trim(),
            ExternalType = type.Trim(),
            Payload = NFCUtils.EncodeToByteArray(payload)
        };

        return true;
    }

    private static bool TryCreateUriValue(string? value, out string normalized)
    {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(value) ||
            !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri) ||
            !IsAllowedScheme(uri.Scheme))
        {
            return false;
        }

        normalized = uri.ToString();
        return true;
    }

    private static bool IsAllowedScheme(string scheme)
        => string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
           string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
           string.Equals(scheme, Uri.UriSchemeMailto, StringComparison.OrdinalIgnoreCase) ||
           string.Equals(scheme, "tel", StringComparison.OrdinalIgnoreCase);

    private static bool IsValidLanguageCode(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
            return false;

        var trimmed = languageCode.Trim();
        return trimmed.Length <= 63 && trimmed.All(character => character is >= ' ' and <= '~');
    }

    private static bool IsValidMimeType(string? mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
            return false;

        var trimmed = mimeType.Trim();
        var slashIndex = trimmed.IndexOf('/');
        return slashIndex > 0 &&
               slashIndex < trimmed.Length - 1 &&
               trimmed.All(character => !char.IsWhiteSpace(character));
    }
}
