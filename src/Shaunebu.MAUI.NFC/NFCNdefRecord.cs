using Shaunebu.MAUI.NFC.Utils;

namespace Shaunebu.MAUI.NFC;

/// <summary>
/// Class describing the information containing within a NFC tag
/// </summary>
public sealed class NFCNdefRecord
{
    private byte[]? _type;
    private byte[]? _identifier;

    /// <summary>
    /// NDEF Type
    /// </summary>
    public NFCNdefTypeFormat TypeFormat { get; set; }

    /// <summary>
    /// MimeType used for <see cref="NFCNdefTypeFormat.Mime"/> type
    /// </summary>
    public string MimeType { get; set; } = "text/plain";

    /// <summary>
    /// External domain used for <see cref="NFCNdefTypeFormat.External"/> type
    /// </summary>
    public string? ExternalDomain { get; set; }

    /// <summary>
    /// External type used for <see cref="NFCNdefTypeFormat.External"/> type
    /// </summary>
    public string? ExternalType { get; set; }

    /// <summary>
    /// Payload
    /// </summary>
    public byte[]? Payload { get; set; } 

    /// <summary>
    /// Raw NDEF type bytes when provided by the platform.
    /// </summary>
    public byte[]? Type
    {
        get => Copy(_type);
        set => _type = Copy(value);
    }

    /// <summary>
    /// Raw NDEF record identifier bytes when provided by the platform.
    /// </summary>
    public byte[]? Identifier
    {
        get => Copy(_identifier);
        set => _identifier = Copy(value);
    }

    /// <summary>
    /// Uri
    /// </summary>
    public string? Uri { get; set; }

    /// <summary>
    /// String formatted payload
    /// </summary>
    public string Message => NFCUtils.GetMessage(this);

    /// <summary>
    /// Two letters ISO 639-1 Language Code (ex: en, fr, de...)
    /// </summary>
    public string? LanguageCode { get; set; }

    private static byte[]? Copy(byte[]? bytes)
        => bytes is null ? null : [.. bytes];
}
