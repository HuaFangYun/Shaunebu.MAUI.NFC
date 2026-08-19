using CoreFoundation;
using CoreNFC;
using Foundation;
using Shaunebu.MAUI.NFC.Configuration;
using Shaunebu.MAUI.NFC.Diagnostics;
using Shaunebu.MAUI.NFC.Utils;
using UIKit;

namespace Shaunebu.MAUI.NFC;

/// <summary>
/// Old iOS implementation of <see cref="INFC"/> (iOS < 13)
/// </summary>
internal sealed class NFCImplementation_Before_iOS13 : NFCNdefReaderSessionDelegate, INFC, INfcCapabilityProvider
{
    public const string SessionTimeoutMessage = "session timeout";

    /// <summary>
    /// Default constructor
    /// </summary>
    public NFCImplementation_Before_iOS13()
    {
    }

    private bool _isWriting;
    private bool _isFormatting;
    private bool _customInvalidation = false;
    private bool _sessionActive;
    private INFCNdefTag? _tag;

    public event EventHandler? OnTagConnected;
    public event EventHandler? OnTagDisconnected;
    public event NdefMessageReceivedEventHandler? OnMessageReceived;
    public event NdefMessagePublishedEventHandler? OnMessagePublished;
    public event TagDiscoveredEventHandler? OnTagDiscovered;
    public event EventHandler? OniOSReadingSessionCancelled;
    public event OnNfcStatusChangedEventHandler? OnNfcStatusChanged;
    public event TagListeningStatusChangedEventHandler? OnTagListeningStatusChanged;

    private NFCNdefReaderSession? NfcSession { get; set; }

    /// <summary>
    /// Checks if NFC Feature is available
    /// </summary>
    public bool IsAvailable => NFCNdefReaderSession.ReadingAvailable;

    /// <summary>
    /// Checks if NFC Feature is enabled
    /// </summary>
    public bool IsEnabled => IsAvailable;

    /// <summary>
    /// Checks if writing mode is supported
    /// </summary>
    public bool IsWritingTagSupported => NFCUtils.IsWritingSupported();

    private NfcOptions _options = new();
    private Microsoft.Extensions.Logging.ILogger? Logger => NfcDiagnostics.CreateLogger(_options);

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public NfcOptions Options
    {
        get => _options;
        set
        {
            _options = value;
            EnableLegacy(_options.LegacyMode);
            SetConfiguration(_options.Configuration);
        }
    }

    /// <summary>
    /// NFC configuration
    /// </summary>
    public NfcConfiguration Configuration { get; } = NfcConfiguration.GetDefaultConfiguration();

    /// <summary>
    /// Set legacy mode
    /// </summary>
    /// <param name="value"></param>
    public void EnableLegacy(bool value)
    {
        if (CrossNFC.IsLegacy == value)
            return;

        CrossNFC.Legacy = value;
    }

    /// <summary>
    /// Update NFC configuration
    /// </summary>
    /// <param name="configuration"><see cref="NfcConfiguration"/></param>
    public void SetConfiguration(NfcConfiguration configuration) => Configuration.Update(configuration);

    /// <summary>
    /// Starts tags detection
    /// </summary>
    public void StartListening()
    {
        if (_sessionActive)
            return;

        _customInvalidation = false;
        _isWriting = false;
        _isFormatting = false;
        _sessionActive = true;

        NfcSession = new NFCNdefReaderSession(this, NSOperationQueue.CurrentQueue.UnderlyingQueue, true)
        {
            AlertMessage = Configuration.Messages.NFCDialogAlertMessage
        };

        try
        {
            NfcSession?.BeginSession();
            NfcDiagnostics.LogEvent(Logger, Microsoft.Extensions.Logging.LogLevel.Information, NfcLogEventIds.ListeningStarted, "iOS", "legacy listening started");
            Raise(OnTagListeningStatusChanged, nameof(OnTagListeningStatusChanged), handler => handler(true));
        }
        catch
        {
            _sessionActive = false;
            NfcSession = null;
            NfcDiagnostics.LogFailure(Logger, NfcLogEventIds.NdefReadFailed, "iOS", "legacy start listening", "BeginSession");
            throw;
        }
    }

    /// <summary>
    /// Stops tags detection
    /// </summary>
    public void StopListening()
    {
        NfcSession?.InvalidateSession();
    }

    /// <summary>
    /// Starts tag publishing (writing or formatting)
    /// </summary>
    /// <param name="clearMessage">Format tag</param>
    public void StartPublishing(bool clearMessage = false)
    {
        if (_sessionActive)
            return;

        if (!IsAvailable)
        {
            throw new InvalidOperationException(Configuration.Messages.NFCWritingNotSupported);
        }

        _customInvalidation = false;
        _isWriting = true;
        _isFormatting = clearMessage;
        _sessionActive = true;

        NfcSession = new NFCNdefReaderSession(this, NSOperationQueue.CurrentQueue.UnderlyingQueue, true)
        {
            AlertMessage = Configuration.Messages.NFCDialogAlertMessage
        };

        try
        {
            NfcSession?.BeginSession();
            NfcDiagnostics.LogEvent(Logger, Microsoft.Extensions.Logging.LogLevel.Information, NfcLogEventIds.ListeningStarted, "iOS", "legacy publishing started");
            Raise(OnTagListeningStatusChanged, nameof(OnTagListeningStatusChanged), handler => handler(true));
        }
        catch
        {
            _sessionActive = false;
            NfcSession = null;
            NfcDiagnostics.LogFailure(Logger, NfcLogEventIds.NdefWriteFailed, "iOS", "legacy start publishing", "BeginSession");
            throw;
        }
    }

    /// <summary>
    /// Stops tag publishing
    /// </summary>
    public void StopPublishing()
    {
        _isWriting = _isFormatting = _customInvalidation = false;
        _tag = null;
        NfcSession?.InvalidateSession();
    }

    /// <summary>
    /// Publish or write a message on a tag
    /// </summary>
    /// <param name="tagInfo">see <see cref="ITagInfo"/></param>
    /// <param name="makeReadOnly">Make a tag read-only</param>
    public void PublishMessage(ITagInfo tagInfo, bool makeReadOnly = false) => WriteOrClearMessage(_tag, tagInfo, false, makeReadOnly);

    /// <summary>
    /// Format tag
    /// </summary>
    /// <param name="tagInfo">see <see cref="ITagInfo"/></param>
    public void ClearMessage(ITagInfo tagInfo) => WriteOrClearMessage(_tag, tagInfo, true);

    /// <summary>
    /// Raises the OnMessageReceived event (for external use)
    /// </summary>
    /// <param name="tagInfo">Tag information</param>
    public void RaiseMessageReceived(ITagInfo tagInfo)
    {
        Raise(OnMessageReceived, nameof(OnMessageReceived), handler => handler(tagInfo));
    }

    /// <summary>
    /// Event raised when NDEF messages are detected
    /// </summary>
    /// <param name="session">iOS <see cref="NFCNdefReaderSession"/></param>
    /// <param name="messages">Array of iOS <see cref="NFCNdefMessage"/></param>
    public override void DidDetect(NFCNdefReaderSession session, NFCNdefMessage[] messages)
    {
        Raise(OnTagConnected, nameof(OnTagConnected), handler => handler(null, EventArgs.Empty));

        if (messages is not null && messages.Length > 0)
        {
            var first = messages[0];
            var tagInfo = new TagInfo
            {
                IsWritable = false,
                Records = NFCNdefPayloadExtensions.GetRecords(first.Records)
            };
            NfcDiagnostics.LogEvent(Logger, Microsoft.Extensions.Logging.LogLevel.Information, NfcLogEventIds.NdefReadSucceeded, "iOS", "legacy NDEF read succeeded");
            Raise(OnMessageReceived, nameof(OnMessageReceived), handler => handler(tagInfo));
        }

        Raise(OnTagDisconnected, nameof(OnTagDisconnected), handler => handler(null, EventArgs.Empty));
    }

    /// <summary>
    /// Event raised when NFC tag detected
    /// </summary>
    /// <param name="session">iOS <see cref="NFCNdefReaderSession"/></param>
    /// <param name="tags">Array of iOS <see cref="INFCNdefTag"/></param>
    public override void DidDetectTags(NFCNdefReaderSession session, INFCNdefTag[] tags)
    {
        _customInvalidation = false;
        var selectionStatus = NfcOperationGuards.SelectSingleTag(tags, out var selectedTag);
        if (selectionStatus != NfcTagSelectionStatus.SingleTag || selectedTag is null)
        {
            if (selectionStatus == NfcTagSelectionStatus.MultipleTags)
                NfcDiagnostics.LogEvent(Logger, Microsoft.Extensions.Logging.LogLevel.Warning, NfcLogEventIds.MultipleTagsDetected, "iOS", "legacy multiple tags detected");

            RetrySingleTagDetection(session, selectionStatus);
            return;
        }

        _tag = selectedTag;

        session.ConnectToTag(_tag, error =>
        {
            if (error is not null)
            {
                Invalidate(session, error.LocalizedDescription);
                return;
            }

            if (_tag is null)
            {
                Invalidate(session, Configuration.Messages.NFCErrorNotCompliantTag);
                return;
            }

            _tag.QueryNdefStatus((status, capacity, ndefError) =>
            {
                if (ndefError is not null)
                {
                    Invalidate(session, ndefError.LocalizedDescription);
                    return;
                }

                var isNdefSupported = status != NFCNdefStatus.NotSupported;

                var identifier = NfcNdefTagExtensions.GetTagIdentifier(_tag);

                identifier ??= [];

                if (identifier is null)
                {
                    Invalidate(session, Configuration.Messages.NFCErrorNotCompliantTag);
                    return;
                }

                var nTag = new TagInfo(identifier, isNdefSupported, technologies: GetTechnologies(_tag), platformTechnologies: GetPlatformTechnologies(_tag))
                {
                    IsWritable = status == NFCNdefStatus.ReadWrite,
                    Capacity = Convert.ToInt32(capacity)
                };

                if (!isNdefSupported)
                {
                    session.AlertMessage = Configuration.Messages.NFCErrorNotSupportedTag;

                    NfcDiagnostics.LogTagDiscovered(Logger, "iOS", nTag);
                    Raise(OnMessageReceived, nameof(OnMessageReceived), handler => handler(nTag));
                    Invalidate(session);
                    return;
                }

                if (_isWriting)
                {
                    // Write mode
                    NfcDiagnostics.LogTagDiscovered(Logger, "iOS", nTag);
                    Raise(OnTagDiscovered, nameof(OnTagDiscovered), handler => handler(nTag, _isFormatting));
                }
                else
                {
                    // Read mode
                    _tag.ReadNdef((message, readError) =>
                    {
                        if (readError is not null)
                        {
                            NfcDiagnostics.LogFailure(Logger, NfcLogEventIds.NdefReadFailed, "iOS", "legacy read", readError.GetType().FullName);
                            Invalidate(session, readError.Code == (int)NFCReaderError.NdefReaderSessionErrorZeroLengthMessage
                                ? Configuration.Messages.NFCErrorEmptyTag
                                : Configuration.Messages.NFCErrorRead);
                            return;
                        }

                        session.AlertMessage = Configuration.Messages.NFCSuccessRead;

                        nTag.Records = NFCNdefPayloadExtensions.GetRecords(message?.Records);
                        NfcDiagnostics.LogEvent(Logger, Microsoft.Extensions.Logging.LogLevel.Information, NfcLogEventIds.NdefReadSucceeded, "iOS", "legacy NDEF read succeeded");
                        Raise(OnMessageReceived, nameof(OnMessageReceived), handler => handler(nTag));
                        Invalidate(session);
                    });
                }
            });
        });
    }

    /// <summary>
    /// Event raised when an error happened during detection
    /// </summary>
    /// <param name="session">iOS <see cref="NFCTagReaderSession"/></param>
    /// <param name="error">iOS <see cref="NSError"/></param>
    public override void DidInvalidate(NFCNdefReaderSession session, NSError error)
    {
        _sessionActive = false;
        _tag = null;
        NfcSession = null;
        _isWriting = false;
        _isFormatting = false;

        NfcDiagnostics.LogEvent(Logger, Microsoft.Extensions.Logging.LogLevel.Information, NfcLogEventIds.SessionInvalidated, "iOS", "legacy session invalidated");
        Raise(OnTagListeningStatusChanged, nameof(OnTagListeningStatusChanged), handler => handler(false));

        var readerError = (NFCReaderError)(long)error.Code;
        if (readerError != NFCReaderError.ReaderSessionInvalidationErrorFirstNDEFTagRead && readerError != NFCReaderError.ReaderSessionInvalidationErrorUserCanceled)
        {
            var alertController = UIAlertController.Create(Configuration.Messages.NFCSessionInvalidated, error.LocalizedDescription.ToLower().Equals(SessionTimeoutMessage) ? Configuration.Messages.NFCSessionTimeout : error.LocalizedDescription, UIAlertControllerStyle.Alert);
            alertController.AddAction(UIAlertAction.Create(Configuration.Messages.NFCSessionInvalidatedButton, UIAlertActionStyle.Default, null));
            Raise(OniOSReadingSessionCancelled, nameof(OniOSReadingSessionCancelled), handler => handler(null, EventArgs.Empty));
            DispatchQueue.MainQueue.DispatchAsync(() =>
            {
                GetCurrentController().PresentViewController(alertController, true, null);
            });
        }
        else if (readerError == NFCReaderError.ReaderSessionInvalidationErrorUserCanceled && !_customInvalidation)
        {
            Raise(OniOSReadingSessionCancelled, nameof(OniOSReadingSessionCancelled), handler => handler(null, EventArgs.Empty));
        }
    }

    /// <summary>
    /// Write or Clear a NDEF message
    /// </summary>
    /// <param name="tag"><see cref="INFCTag"/></param>
    /// <param name="tagInfo"><see cref="ITagInfo"/></param>
    /// <param name="clearMessage">Clear Message</param>
    /// <param name="makeReadOnly">Make a tag read-only</param>
    internal void WriteOrClearMessage(INFCNdefTag? tag, ITagInfo? tagInfo, bool clearMessage = false, bool makeReadOnly = false)
    {
        if (NfcSession is null)
        {
            return;
        }

        if (tag is null)
        {
            Invalidate(NfcSession, Configuration.Messages.NFCErrorMissingTag);
            return;
        }

        if (tagInfo is null || (!clearMessage && tagInfo.Records.Any(record => record.Payload is null)))
        {
            Invalidate(NfcSession, Configuration.Messages.NFCErrorMissingTagInfo);
            return;
        }

        if (_tag is null)
        {
            Invalidate(NfcSession, Configuration.Messages.NFCErrorNotCompliantTag);
            return;
        }

        try
        {
            if (!_tag.Available)
            {
                NfcSession.ConnectToTag(_tag, (error) =>
                {
                    if (error is not null)
                    {
                        Invalidate(NfcSession, error.LocalizedDescription);
                        return;
                    }

                    ExecuteWriteOrClear(NfcSession, _tag, tagInfo, clearMessage, makeReadOnly);
                });
            }
            else
            {
                ExecuteWriteOrClear(NfcSession, _tag, tagInfo, clearMessage, makeReadOnly);
            }
        }
        catch (Exception ex)
        {
            NfcDiagnostics.LogFailure(Logger, NfcLogEventIds.NdefWriteFailed, "iOS", "legacy write", ex);
            Invalidate(NfcSession, ex.Message);
        }
        finally
        {
            Raise(OnTagDisconnected, nameof(OnTagDisconnected), handler => handler(null, EventArgs.Empty));
        }
    }

    #region Private

    /// <summary>
    /// Returns the current iOS controller
    /// </summary>
    /// <returns>Object <see cref="UIViewController"/></returns>
    private static UIViewController GetCurrentController()
    {
#if IOS15_0_OR_GREATER
        var window = UIApplication.SharedApplication.ConnectedScenes.OfType<UIWindowScene>().SelectMany(s => s.Windows).First(w => w.IsKeyWindow);
#else
        var window = UIApplication.SharedApplication.Windows.FirstOrDefault(w => w.IsKeyWindow);
#endif
        var vc = window.RootViewController!;

        while (vc.PresentedViewController is not null)
            vc = vc.PresentedViewController;

        return vc;
    }

    /// <summary>
    /// Writes or clears a TAG
    /// </summary>
    /// <param name="session"><see cref="NFCTagReaderSession"/></param>
    /// <param name="tag"><see cref="INFCNdefTag"/></param>
    /// <param name="tagInfo"><see cref="ITagInfo"/></param>
    /// <param name="clearMessage">Clear message</param>
    private void ExecuteWriteOrClear(NFCNdefReaderSession session, INFCNdefTag tag, ITagInfo tagInfo, bool clearMessage = false, bool makeReadOnly = false)
        => tag.QueryNdefStatus((status, capacity, error) =>
        {
            if (error is not null)
            {
                Invalidate(session, error.LocalizedDescription);
                return;
            }

            if (status == NFCNdefStatus.ReadOnly)
            {
                Invalidate(session, Configuration.Messages.NFCErrorReadOnlyTag);
                return;
            }

            NFCNdefMessage? message = null;
            if (!clearMessage)
            {
                session.AlertMessage = Configuration.Messages.NFCSuccessWrite;

                var records = new List<NFCNdefPayload>();

                foreach (var record in tagInfo.Records)
                {
                    if (NFCNdefPayloadExtensions.GetiOSPayload(record, Configuration) is NFCNdefPayload ndefPayload)
                        records.Add(ndefPayload);
                }

                if (records.Any())
                    message = new NFCNdefMessage(records.ToArray());
            }
            else
            {
                session.AlertMessage = Configuration.Messages.NFCSuccessClear;
                message = NFCNdefMessageExtensions.EmptyNdefMessage;
            }

            if (message is not null)
            {
                if (Convert.ToInt32(capacity) < message.ToByteArray().Length)
                {
                    Invalidate(session, Configuration.Messages.NFCErrorCapacityTag);
                    return;
                }

                tag.WriteNdef(message, (error) =>
                {
                    if (error is not null)
                    {
                        NfcDiagnostics.LogFailure(Logger, NfcLogEventIds.NdefWriteFailed, "iOS", "legacy write", error.GetType().FullName);
                        Invalidate(session, error.LocalizedDescription);
                        return;
                    }

                    if (!clearMessage && makeReadOnly)
                    {
                        MakeTagReadOnly(session, tag, () => CompleteWrite(session, tagInfo, message));
                        return;
                    }

                    CompleteWrite(session, tagInfo, message);
                });
            }
            else
                Invalidate(session, Configuration.Messages.NFCErrorWrite);
        });

    private void CompleteWrite(NFCNdefReaderSession session, ITagInfo tagInfo, NFCNdefMessage message)
    {
        tagInfo.Records = NFCNdefPayloadExtensions.GetRecords(message.Records);
        NfcDiagnostics.LogEvent(Logger, Microsoft.Extensions.Logging.LogLevel.Information, NfcLogEventIds.NdefWriteSucceeded, "iOS", "legacy NDEF write succeeded");
        Raise(OnMessagePublished, nameof(OnMessagePublished), handler => handler(tagInfo));
        Invalidate(session);
    }

    /// <summary>
    /// Make a tag read-only
    /// WARNING: This operation is permanent
    /// </summary>
    /// <param name="session"><see cref="NFCTagReaderSession"/></param>
    /// <param name="tag"><see cref="ITagInfo"/></param>
    /// <param name="ndefTag"><see cref="INFCNdefTag"/></param>
    private void MakeTagReadOnly(NFCNdefReaderSession session, INFCNdefTag tag, Action onSuccess)
        => session.ConnectToTag(tag, (error) =>
        {
            if (error is not null)
            {
                NfcDiagnostics.LogFailure(Logger, NfcLogEventIds.ReadOnlyLockFailed, "iOS", "legacy read-only lock", error.GetType().FullName);
                Invalidate(session, NfcOperationGuards.CombineWriteLockFailure(error.LocalizedDescription));
                return;
            }

            tag.WriteLock((error) =>
            {
                if (error is not null)
                {
                    NfcDiagnostics.LogFailure(Logger, NfcLogEventIds.ReadOnlyLockFailed, "iOS", "legacy read-only lock", error.GetType().FullName);
                    Invalidate(session, NfcOperationGuards.CombineWriteLockFailure(error.LocalizedDescription));
                }
                else
                {
                    NfcDiagnostics.LogEvent(Logger, Microsoft.Extensions.Logging.LogLevel.Information, NfcLogEventIds.ReadOnlyLockSucceeded, "iOS", "legacy read-only lock succeeded");
                    onSuccess();
                }
            });
        });

    public NfcCapabilities GetCapabilities()
    {
        var capabilities = IsAvailable
            ? new NfcCapabilities
            {
                Availability = NfcAvailabilityStatus.Available,
                CanRead = true,
                CanWrite = IsWritingTagSupported,
                CanMakeReadOnly = IsWritingTagSupported
            }
            : new NfcCapabilities
            {
                Availability = NfcAvailabilityStatus.Unknown,
                PlatformReason = "Core NFC reports reading unavailable; device support, entitlement, signing, simulator state, and temporary platform state cannot be separated by this API."
            };

        NfcDiagnostics.LogCapabilities(Logger, "iOS", capabilities);
        return capabilities;
    }

    private void Raise<TDelegate>(TDelegate? handlers, string eventName, Action<TDelegate> invokeSubscriber)
        where TDelegate : Delegate
        => NfcEventDispatcher.Invoke(handlers, eventName, _options, Logger, invokeSubscriber);

    private static IReadOnlyList<NfcTagTechnology> GetTechnologies(INFCNdefTag tag)
    {
        var technologies = new List<NfcTagTechnology> { NfcTagTechnology.Ndef };

        if (tag is INFCMiFareTag)
            technologies.Add(NfcTagTechnology.Mifare);
        if (tag is INFCFeliCaTag)
            technologies.Add(NfcTagTechnology.FeliCa);
        if (tag is INFCIso15693Tag)
            technologies.Add(NfcTagTechnology.Iso15693);
        if (tag is INFCIso7816Tag)
            technologies.Add(NfcTagTechnology.Iso7816);

        return technologies;
    }

    private static IReadOnlyList<string> GetPlatformTechnologies(INFCNdefTag tag)
    {
        var technologies = new List<string> { "NDEF" };

        if (tag is INFCMiFareTag)
            technologies.Add("MiFare");
        if (tag is INFCFeliCaTag)
            technologies.Add("FeliCa");
        if (tag is INFCIso15693Tag)
            technologies.Add("Iso15693");
        if (tag is INFCIso7816Tag)
            technologies.Add("Iso7816Compatible");

        return technologies;
    }

    private void RetrySingleTagDetection(NFCNdefReaderSession session, NfcTagSelectionStatus selectionStatus)
    {
        _tag = null;
        if (selectionStatus == NfcTagSelectionStatus.MultipleTags)
            session.AlertMessage = NfcOperationGuards.MultipleTagsDetectedMessage;

        session.RestartPolling();
    }

    /// <summary>
    /// Invalidate the session
    /// </summary>
    /// <param name="session"><see cref="NFCTagReaderSession"/></param>
    /// <param name="message">Message to show</param>
    private void Invalidate(NFCNdefReaderSession session, string? message = null)
    {
        _customInvalidation = true;
        if (string.IsNullOrWhiteSpace(message))
            session.InvalidateSession();
        else
            session.InvalidateSession(message);
    }

    #endregion
}
