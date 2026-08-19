using System.Text;
#if IOS
using UIKit;
#endif

namespace Shaunebu.MAUI.NFC.Utils;

/// <summary>
/// NFC tools
/// </summary>
public static class NFCUtils
{
    /// <summary>
    /// Returns the content size of an array of <see cref="NFCNdefRecord"/>
    /// </summary>
    /// <param name="records">array of <see cref="NFCNdefRecord"/></param>
    /// <returns>Content size</returns>
    internal static int GetSize(NFCNdefRecord[] records)
        => NdefRecordEncoding.GetEncodedMessageSize(records);

    /// <summary>
    /// Returns the string formatted payload
    /// </summary>
    /// <param name="type">type of <see cref="NFCNdefTypeFormat"/></param>
    /// <param name="payload">record payload</param>
    /// <param name="uri">record uri</param>
    /// <returns>String formatted payload</returns>
    internal static string GetMessage(NFCNdefTypeFormat type, byte[]? payload, string? uri)
    {
        var message = string.Empty;

        if (!string.IsNullOrWhiteSpace(uri))
        {
            message = uri;
        }
        else if (payload is not null)
        {
            if (type == NFCNdefTypeFormat.WellKnown)
            {
                if (NdefRecordEncoding.TryParseTextPayload(payload, out var textMessage, out _))
                    message = textMessage;
            }
            else if (NdefRecordEncoding.TryDecodeUtf8(payload, out var decodedPayload))
            {
                // Other NDEF types
                message = decodedPayload;
            }
        }

        return message;
    }

    /// <summary>
    /// Transforms a string message into an array of bytes
    /// </summary>
    /// <param name="text">text message</param>
    /// <returns>Array of bytes</returns>
    public static byte[] EncodeToByteArray(string text) => Encoding.UTF8.GetBytes(text);

    /// <summary>
    /// Returns the string formatted payload
    /// </summary>
    /// <param name="record">Object <see cref="NFCNdefRecord"/></param>
    /// <returns>String formatted payload</returns>
    public static string GetMessage(NFCNdefRecord record)
    {
        if (record == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(record.Uri))
            return record.Uri;

        if (record.Payload is null)
            return string.Empty;

        if (NdefRecordEncoding.IsTextRecord(record))
        {
            return NdefRecordEncoding.TryParseTextPayload(record.Payload, out var message, out _)
                ? message
                : string.Empty;
        }

        return record.TypeFormat == NFCNdefTypeFormat.WellKnown
            ? string.Empty
            : GetMessage(record.TypeFormat, record.Payload, record.Uri);
    }

    /// <summary>
    /// Convert bytes array into hexadecimal string
    /// </summary>
    /// <param name="bytes">Bytes Array</param>
    /// <param name="separator">Separator</param>
    /// <returns>Hexadecimal string</returns>
    public static string ByteArrayToHexString(byte[] bytes, string? separator = null)
        => bytes == null ? string.Empty : string.Join(separator ?? string.Empty, bytes.Select(b => b.ToString("X2")));

    /// <summary>
    /// Checks if writing tags is supported
    /// </summary>
    /// <returns>boolean</returns>
    public static bool IsWritingSupported()
    {
#if IOS
        var splitted = UIDevice.CurrentDevice.SystemVersion?.Split('.');
        if (splitted != null && splitted.Length > 0 && int.TryParse(splitted[0], out var majorVersion))
            return majorVersion >= 13;
        return false;
#else
        return true;
#endif
    }
}
