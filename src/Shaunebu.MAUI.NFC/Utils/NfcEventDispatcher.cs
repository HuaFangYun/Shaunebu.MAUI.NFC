using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Shaunebu.MAUI.NFC.Configuration;

namespace Shaunebu.MAUI.NFC.Utils;

internal static class NfcEventDispatcher
{
    internal static void Invoke<TDelegate>(
        TDelegate? handlers,
        string eventName,
        NfcOptions options,
        ILogger? logger,
        Action<TDelegate> invokeSubscriber)
        where TDelegate : Delegate
    {
        if (handlers is null)
            return;

        var subscribers = handlers.GetInvocationList().Cast<TDelegate>().ToArray();
        if (subscribers.Length == 0)
            return;

        void InvokeAll()
        {
            foreach (var subscriber in subscribers)
            {
                try
                {
                    invokeSubscriber(subscriber);
                }
                catch (Exception ex)
                {
                    NfcDiagnostics.LogSubscriberFailure(logger, eventName, ex);
                }
            }
        }

        if (options.EventDispatchMode == NfcEventDispatchMode.MainThread && !MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(InvokeAll);
            return;
        }

        InvokeAll();
    }
}
