namespace Shaunebu.MAUI.NFC.Maui.Sample.Models;

public sealed class SampleEventLogEntry
{
    public required string Time { get; init; }
    public required string Kind { get; init; }
    public required string Message { get; init; }

    public string Summary => $"[{Time}] {Kind}: {Message}";

    public static SampleEventLogEntry Create(string kind, string message)
        => new()
        {
            Time = DateTime.Now.ToString("HH:mm:ss"),
            Kind = kind,
            Message = message
        };
}
