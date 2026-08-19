namespace Shaunebu.MAUI.NFC.Configuration;

/// <summary>
/// Controls how Shaunebu.MAUI.NFC invokes public event subscribers.
/// </summary>
public enum NfcEventDispatchMode
{
    /// <summary>
    /// Invoke events on the platform/native callback thread.
    /// This preserves the original event-delivery model.
    /// </summary>
    Native,

    /// <summary>
    /// Marshal public event subscribers to the MAUI main thread.
    /// </summary>
    MainThread
}
