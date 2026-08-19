using Shaunebu.MAUI.NFC.Utils;

namespace Shaunebu.MAUI.NFC.Maui.Sample.Models;

public sealed class NdefRecordDisplay
{
    public required int Index { get; init; }
    public required string TypeFormat { get; init; }
    public required string Message { get; init; }
    public required string Uri { get; init; }
    public required string MimeType { get; init; }
    public required string ExternalType { get; init; }
    public required string PayloadSummary { get; init; }
    public required string TypeSummary { get; init; }
    public required string IdentifierSummary { get; init; }
    public required string HexPreview { get; init; }

    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool HasUri => !string.IsNullOrWhiteSpace(Uri);
    public bool HasMimeType => !string.IsNullOrWhiteSpace(MimeType);
    public bool HasExternalType => !string.IsNullOrWhiteSpace(ExternalType);
    public bool HasTypeSummary => !string.IsNullOrWhiteSpace(TypeSummary);
    public bool HasIdentifierSummary => !string.IsNullOrWhiteSpace(IdentifierSummary);
    public bool HasHexPreview => !string.IsNullOrWhiteSpace(HexPreview);

    public static IReadOnlyList<NdefRecordDisplay> FromRecords(IEnumerable<NFCNdefRecord>? records)
    {
        if (records is null)
            return [];

        return records
            .Select((record, index) => FromRecord(record, index + 1))
            .ToArray();
    }

    private static NdefRecordDisplay FromRecord(NFCNdefRecord? record, int index)
    {
        if (record is null)
        {
            return new NdefRecordDisplay
            {
                Index = index,
                TypeFormat = "Unknown",
                Message = string.Empty,
                Uri = string.Empty,
                MimeType = string.Empty,
                ExternalType = string.Empty,
                PayloadSummary = "Payload: unavailable",
                TypeSummary = string.Empty,
                IdentifierSummary = string.Empty,
                HexPreview = string.Empty
            };
        }

        var payload = record.Payload ?? [];

        return new NdefRecordDisplay
        {
            Index = index,
            TypeFormat = record.TypeFormat.ToString(),
            Message = record.Message,
            Uri = record.Uri ?? string.Empty,
            MimeType = record.MimeType,
            ExternalType = FormatExternalType(record.ExternalDomain, record.ExternalType),
            PayloadSummary = $"Payload: {payload.Length} bytes",
            TypeSummary = FormatBytes("Type", record.Type),
            IdentifierSummary = FormatBytes("Record ID", record.Identifier),
            HexPreview = FormatHexPreview(payload)
        };
    }

    private static string FormatExternalType(string? domain, string? type)
        => string.IsNullOrWhiteSpace(domain) || string.IsNullOrWhiteSpace(type)
            ? string.Empty
            : $"{domain}:{type}";

    private static string FormatHexPreview(byte[] payload)
    {
        if (payload.Length == 0)
            return string.Empty;

        var preview = payload.Take(16).ToArray();
        var suffix = payload.Length > preview.Length ? " ..." : string.Empty;
        return $"Hex: {NFCUtils.ByteArrayToHexString(preview, " ")}{suffix}";
    }

    private static string FormatBytes(string label, byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0)
            return string.Empty;

        var preview = bytes.Take(16).ToArray();
        var suffix = bytes.Length > preview.Length ? " ..." : string.Empty;
        return $"{label}: {NFCUtils.ByteArrayToHexString(preview, " ")}{suffix}";
    }
}
