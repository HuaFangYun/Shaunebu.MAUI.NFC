using CoreFoundation;
using CoreNFC;
using Foundation;
using Shaunebu.MAUI.NFC.Configuration;
using Shaunebu.MAUI.NFC.Diagnostics;
using Shaunebu.MAUI.NFC.Utils;
using System.Text;
using UIKit;

namespace Shaunebu.MAUI.NFC;

/// <summary>
/// iOS 13+ implementation of <see cref="INFC"/>
/// </summary>
internal sealed class NFCImplementation_iOS : NFCTagReaderSessionDelegate, INFC, INfcCapabilityProvider
{
    public const string SessionTimeoutMessage = "session timeout";

    /// <summary>
    /// Default constructor
    /// </summary>
    public NFCImplementation_iOS()
    {
    }

    public event EventHandler? OnTagConnected;
    public event EventHandler? OnTagDisconnected;
    public event NdefMessageReceivedEventHandler? OnMessageReceived;
    public event NdefMessagePublishedEventHandler? OnMessagePublished;
    public event TagDiscoveredEventHandler? OnTagDiscovered;
    public event EventHandler? OniOSReadingSessionCancelled;
    public event OnNfcStatusChangedEventHandler? OnNfcStatusChanged;
    public event TagListeningStatusChangedEventHandler? OnTagListeningStatusChanged;

    private bool _isWriting;
    private bool _isFormatting;
    private bool _customInvalidation = false;
    private bool _sessionActive;

    private INFCTag? _tag;

    private NFCTagReaderSession? NfcSession { get; set; }

    /// <summary>
    /// Checks if NFC Feature is available
    /// </summary>
    public bool IsAvailable => NFCReaderSession.ReadingAvailable;

    /// <summary>
    /// Checks if NFC Feature is enabled
    /// </summary>
    public bool IsEnabled => IsAvailable;

    /// <summary>
    /// Checks if writing mode is supported
    /// </summary>
    public bool IsWritingTagSupported => true;

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

        NfcSession = new NFCTagReaderSession(NFCPollingOption.Iso14443 | NFCPollingOption.Iso15693, this, NSOperationQueue.CurrentQueue.UnderlyingQueue)
        {
            AlertMessage = Configuration.Messages.NFCDialogAlertMessage
        };

        try
        {
            NfcSession.BeginSession();
            NfcDiagnostics.LogEvent(Logger, Microsoft.Extensions.Logging.LogLevel.Information, NfcLogEventIds.ListeningStarted, "iOS", "listening started");
            Raise(OnTagListeningStatusChanged, nameof(OnTagListeningStatusChanged), handler => handler(true));
        }
        catch
        {
            _sessionActive = false;
            NfcSession = null;
            NfcDiagnostics.LogFailure(Logger, NfcLogEventIds.NdefReadFailed, "iOS", "start listening", "BeginSession");
            throw;
        }
    }

    /// <summary>
    /// Stops tags detection
    /// </summary>
    public void StopListening() => NfcSession?.InvalidateSession();

    /// <summary>
    /// Starts tag publishing (writing or formatting)
    /// </summary>
    /// <param name="clearMessage">Format tag</param>
    public void StartPublishing(bool clearMessage = false)
    {
        if (_sessionActive)
            return;

        if (!IsAvailable)
            throw new InvalidOperationException(Configuration.Messages.NFCWritingNotSupported);

        _customInvalidation = false;
        _isWriting = true;
        _isFormatting = clearMessage;
        _sessionActive = true;

        NfcSession = new NFCTagReaderSession(NFCPollingOption.Iso14443 | NFCPollingOption.Iso15693, this, NSOperationQueue.CurrentQueue.UnderlyingQueue)
        {
            AlertMessage = Configuration.Messages.NFCDialogAlertMessage
        };

        try
        {
            NfcSession.BeginSession();
            NfcDiagnostics.LogEvent(Logger, Microsoft.Extensions.Logging.LogLevel.Information, NfcLogEventIds.ListeningStarted, "iOS", "publishing started");
            Raise(OnTagListeningStatusChanged, nameof(OnTagListeningStatusChanged), handler => handler(true));
        }
        catch
        {
            _sessionActive = false;
            NfcSession = null;
            NfcDiagnostics.LogFailure(Logger, NfcLogEventIds.NdefWriteFailed, "iOS", "start publishing", "BeginSession");
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
    /// <param name="makeReadOnly">make tag read-only</param>
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
    /// Event raised when NFC tags are detected
    /// </summary>
    /// <param name="session">iOS <see cref="NFCTagReaderSession"/></param>
    /// <param name="tags">Array of iOS <see cref="INFCTag"/></param>
    public override void DidDetectTags(NFCTagReaderSession session, INFCTag[] tags)
    {
        _customInvalidation = false;
        var selectionStatus = NfcOperationGuards.SelectSingleTag(tags, out var selectedTag);
        if (selectionStatus != NfcTagSelectionStatus.SingleTag || selectedTag is null)
        {
            if (selectionStatus == NfcTagSelectionStatus.MultipleTags)
                NfcDiagnostics.LogEvent(Logger, Microsoft.Extensions.Logging.LogLevel.Warning, NfcLogEventIds.MultipleTagsDetected, "iOS", "multiple tags detected");

            RetrySingleTagDetection(session, selectionStatus);
            return;
        }

        _tag = selectedTag;

        var connectionError = string.Empty;
        session.ConnectTo(_tag, (error) =>
        {
            if (error is not null)
            {
                connectionError = error.LocalizedDescription;
                Invalidate(session, connectionError);
                return;
            }

            var ndefTag = NfcNdefTagExtensions.GetNdefTag(_tag);

            if (ndefTag is null)
            {
                Invalidate(session, Configuration.Messages.NFCErrorNotCompliantTag);
                return;
            }

            ndefTag.QueryNdefStatus((status, capacity, error) =>
            {
                if (error is not null)
                {
                    Invalidate(session, error.LocalizedDescription);
                    return;
                }

                var isNdefSupported = status != NFCNdefStatus.NotSupported;

                var identifier = NfcNdefTagExtensions.GetTagIdentifier(ndefTag);

                if (identifier is null)
                {
                    Invalidate(session, Configuration.Messages.NFCErrorNotCompliantTag);
                    return;
                }

                var nTag = new TagInfo(identifier, isNdefSupported, technologies: GetTechnologies(selectedTag), platformTechnologies: GetPlatformTechnologies(selectedTag))
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
                    ndefTag.ReadNdef((message, error) =>
                    {
                        // iOS Error: NFCReaderError.NdefReaderSessionErrorZeroLengthMessage (NDEF tag does not contain any NDEF message)
                        // NFCReaderError.NdefReaderSessionErrorZeroLengthMessage constant should be equals to 403 instead of 304
                        // see https://developer.apple.com/documentation/corenfc/nfcreadererror/code/ndefreadersessionerrorzerolengthmessage
                        if (error is not null && error.Code != 403)
                        {
                            NfcDiagnostics.LogFailure(Logger, NfcLogEventIds.NdefReadFailed, "iOS", "read", error.GetType().FullName);
                            Invalidate(session, Configuration.Messages.NFCErrorRead);
                            return;
                        }

                        session.AlertMessage = Configuration.Messages.NFCSuccessRead;

                        nTag.Records = NFCNdefPayloadExtensions.GetRecords(message?.Records);
                        NfcDiagnostics.LogEvent(Logger, Microsoft.Extensions.Logging.LogLevel.Information, NfcLogEventIds.NdefReadSucceeded, "iOS", "NDEF read succeeded");
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
    public override void DidInvalidate(NFCTagReaderSession session, NSError error)
    {
        _sessionActive = false;
        _tag = null;
        NfcSession = null;
        _isWriting = false;
        _isFormatting = false;

        NfcDiagnostics.LogEvent(Logger, Microsoft.Extensions.Logging.LogLevel.Information, NfcLogEventIds.SessionInvalidated, "iOS", "session invalidated");
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
            Raise(OniOSReadingSessionCancelled, nameof(OniOSReadingSessionCancelled), handler => handler(null, EventArgs.Empty));
    }

    /// <summary>
    /// Write or Clear a NDEF message
    /// </summary>
    /// <param name="tag"><see cref="INFCTag"/></param>
    /// <param name="tagInfo"><see cref="ITagInfo"/></param>
    /// <param name="clearMessage">Clear Message</param>
    /// <param name="makeReadOnly">Make a tag read-only</param>
    internal void WriteOrClearMessage(INFCTag? tag, ITagInfo? tagInfo, bool clearMessage = false, bool makeReadOnly = false)
    {
        if (NfcSession is null)
            return;

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

        var ndefTag = NfcNdefTagExtensions.GetNdefTag(tag);
        if (ndefTag is null)
        {
            Invalidate(NfcSession, Configuration.Messages.NFCErrorNotCompliantTag);
            return;
        }

        try
        {
            if (!ndefTag.Available)
            {
                NfcSession.ConnectTo(tag, (error) =>
                {
                    if (error is not null)
                    {
                        Invalidate(NfcSession, error.LocalizedDescription);
                        return;
                    }

                    ExecuteWriteOrClear(NfcSession, tag, ndefTag, tagInfo, clearMessage, makeReadOnly);
                });
            }
            else
            {
                ExecuteWriteOrClear(NfcSession, tag, ndefTag, tagInfo, clearMessage, makeReadOnly);
            }
        }
        catch (Exception ex)
        {
            Invalidate(NfcSession, ex.Message);
        }
        finally
        {
            Raise(OnTagDisconnected, nameof(OnTagDisconnected), handler => handler(null, EventArgs.Empty));
        }
    }

    /// <summary>
    /// Returns the current iOS controller
    /// </summary>
    /// <returns>Object <see cref="UIViewController"/></returns>
    private static UIViewController GetCurrentController()
    {
#if IOS15_0_OR_GREATER
        var window = UIApplication.SharedApplication.ConnectedScenes.OfType<UIWindowScene>().SelectMany(s => s.Windows).First(w => w.IsKeyWindow);
#else
        var window = UIApplication.SharedApplication.Windows.First(w => w.IsKeyWindow);
#endif
        var vc = window.RootViewController!;

        while (vc.PresentedViewController is not null)
            vc = vc.PresentedViewController;
        
        return vc;
    }

    /// <summary>
    /// Invalidate the session
    /// </summary>
    /// <param name="session"><see cref="NFCTagReaderSession"/></param>
    /// <param name="message">Message to show</param>
    private void Invalidate(NFCTagReaderSession session, string? message = null)
    {
        _customInvalidation = true;
        if (string.IsNullOrWhiteSpace(message))
            session.InvalidateSession();
        else
            session.InvalidateSession(message);
    }

    private void RetrySingleTagDetection(NFCTagReaderSession session, NfcTagSelectionStatus selectionStatus)
    {
        _tag = null;
        if (selectionStatus == NfcTagSelectionStatus.MultipleTags)
            session.AlertMessage = NfcOperationGuards.MultipleTagsDetectedMessage;

        session.RestartPolling();
    }

    /// <summary>
    /// Writes or clears a TAG
    /// </summary>
    /// <param name="session"><see cref="NFCTagReaderSession"/></param>
    /// <param name="tag"><see cref="INFCNdefTag"/></param>
    /// <param name="tagInfo"><see cref="ITagInfo"/></param>
    /// <param name="clearMessage">Clear message</param>
    private void ExecuteWriteOrClear(NFCTagReaderSession session, INFCTag sessionTag, INFCNdefTag tag, ITagInfo tagInfo, bool clearMessage = false, bool makeReadOnly = false)
        => tag.QueryNdefStatus((status, capacity, error) =>
        {
            if (error is not null)
            {
                    NfcDiagnostics.LogFailure(Logger, NfcLogEventIds.NdefWriteFailed, "iOS", "write", error.GetType().FullName);
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

                if (records.Count > 0)
                    message = new NFCNdefMessage([.. records]);
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
                        Invalidate(session, error.LocalizedDescription);
                        return;
                    }

                    if (!clearMessage && makeReadOnly)
                    {
                        MakeTagReadOnly(session, sessionTag, tag, () => CompleteWrite(session, tagInfo, message));
                        return;
                    }

                    CompleteWrite(session, tagInfo, message);
                });
            }
            else
                Invalidate(session, Configuration.Messages.NFCErrorWrite);
        });

    private void CompleteWrite(NFCTagReaderSession session, ITagInfo tagInfo, NFCNdefMessage message)
    {
        tagInfo.Records = NFCNdefPayloadExtensions.GetRecords(message.Records);
        NfcDiagnostics.LogEvent(Logger, Microsoft.Extensions.Logging.LogLevel.Information, NfcLogEventIds.NdefWriteSucceeded, "iOS", "NDEF write succeeded");
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
    private void MakeTagReadOnly(NFCTagReaderSession session, INFCTag tag, INFCNdefTag ndefTag, Action onSuccess)
        => session.ConnectTo(tag, (error) =>
        {
            if (error is not null)
            {
                NfcDiagnostics.LogFailure(Logger, NfcLogEventIds.ReadOnlyLockFailed, "iOS", "read-only lock", error.GetType().FullName);
                Invalidate(session, NfcOperationGuards.CombineWriteLockFailure(error.LocalizedDescription));
                return;
            }

            ndefTag.WriteLock((error) =>
            {
                if (error is not null)
                {
                    NfcDiagnostics.LogFailure(Logger, NfcLogEventIds.ReadOnlyLockFailed, "iOS", "read-only lock", error.GetType().FullName);
                    Invalidate(session, NfcOperationGuards.CombineWriteLockFailure(error.LocalizedDescription));
                }
                else
                {
                    NfcDiagnostics.LogEvent(Logger, Microsoft.Extensions.Logging.LogLevel.Information, NfcLogEventIds.ReadOnlyLockSucceeded, "iOS", "read-only lock succeeded");
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

    private static IReadOnlyList<NfcTagTechnology> GetTechnologies(INFCTag tag)
        => tag.Type switch
        {
            NFCTagType.MiFare => [NfcTagTechnology.Mifare],
            NFCTagType.Iso7816Compatible => [NfcTagTechnology.Iso7816],
            NFCTagType.Iso15693 => [NfcTagTechnology.Iso15693],
            NFCTagType.FeliCa => [NfcTagTechnology.FeliCa],
            _ => []
        };

    private static IReadOnlyList<string> GetPlatformTechnologies(INFCTag tag)
        => tag.Type switch
        {
            NFCTagType.MiFare => ["MiFare"],
            NFCTagType.Iso7816Compatible => ["Iso7816Compatible"],
            NFCTagType.Iso15693 => ["Iso15693"],
            NFCTagType.FeliCa => ["FeliCa"],
            _ => []
        };
}

/// <summary>
/// NFC Ndef Message extensions class
/// </summary>
internal static class NFCNdefMessageExtensions
{
    /// <summary>
    /// Convert an iOS <see cref="NSData"/> into an array of bytes
    /// </summary>
    /// <param name="data">iOS <see cref="NSData"/></param>
    /// <returns>Array of bytes</returns>
    public static byte[] ToByteArray(this NSData data)
    {
        var bytes = new byte[data.Length];
        if (data.Length > 0) System.Runtime.InteropServices.Marshal.Copy(data.Bytes, bytes, 0, Convert.ToInt32(data.Length));
        return bytes;
    }

    /// <summary>
    /// Converte an iOS <see cref="NFCNdefMessage"/> into an array of bytes
    /// </summary>
    /// <param name="message">iOS <see cref="NFCNdefMessage"/></param>
    /// <returns>Array of bytes</returns>
    public static byte[] ToByteArray(this NFCNdefMessage message)
    {
        var records = message.Records;

        // Empty message: single empty record
        if (records is null || records.Length == 0)
        {
            records = [];
        }

        var m = new MemoryStream();
        for (var i = 0; i < records.Length; i++)
        {
            var record = records[i];
            var typeNameFormat = record?.TypeNameFormat ?? NFCTypeNameFormat.Empty;
            var payload = record?.Payload;
            var id = record?.Identifier;
            var type = record?.Type;

            var flags = (byte)typeNameFormat;

            // Message begin / end flags. If there is only one record in the message, both flags are set.
            if (i == 0)
                flags |= 0x80;      // MB (message begin = first record in the message)

            if (i == records.Length - 1)
                flags |= 0x40;      // ME (message end = last record in the message)

            // cf (chunked records) not supported yet

            // SR (Short Record)?
            if (payload is null || payload.Length <= byte.MaxValue)
                flags |= 0x10;

            // ID present?
            if (id is not null && id.Length > 0)
                flags |= 0x08;

            m.WriteByte(flags);

            // Type length
            if (type is not null)
                m.WriteByte((byte)type.Length);
            else
                m.WriteByte(0);

            // Payload length 1 byte (SR) or 4 bytes
            if (payload is null)
            {
                m.WriteByte(0);
            }
            else
            {
                if ((flags & 0x10) != 0)
                {
                    // SR
                    m.WriteByte((byte)payload.Length);
                }
                else
                {
                    // No SR (Short Record)
                    var payloadLength = (uint)payload.Length;
                    m.WriteByte((byte)(payloadLength >> 24));
                    m.WriteByte((byte)(payloadLength >> 16));
                    m.WriteByte((byte)(payloadLength >> 8));
                    m.WriteByte((byte)(payloadLength & 0x000000ff));
                }
            }

            // ID length
            if (id is not null && (flags & 0x08) != 0)
                m.WriteByte((byte)id.Length);

            // Type length
            if (type is not null && type.Length > 0)
                m.Write([.. type], 0, (int)type.Length);

            // ID data
            if (id is not null && id.Length > 0)
                m.Write([.. id], 0, (int)id.Length);

            // Payload data
            if (payload is not null && payload.Length > 0)
                m.Write([.. payload], 0, (int)payload.Length);
        }

        return m.ToArray();
    }

    /// <summary>
    /// Returns an empty iOS <see cref="NFCNdefMessage"/>
    /// </summary>
    /// <returns>iOS <see cref="NFCNdefMessage"/></returns>
    internal static NFCNdefMessage EmptyNdefMessage
    {
        get
        {
            var records = new NFCNdefPayload[1];
            records[0] = NFCNdefPayloadExtensions.EmptyPayload;
            return new NFCNdefMessage(records);
        }
    }
}

/// <summary>
/// NFC Ndef Payload Extensions Class
/// </summary>
internal static class NFCNdefPayloadExtensions
{
    /// <summary>
    /// Returns ndef payload into MimeType
    /// </summary>
    /// <param name="payload"><see cref="NFCNdefPayload"/></param>
    /// <returns>string</returns>
    public static string? ToMimeType(this NFCNdefPayload payload)
    {
        switch (payload.TypeNameFormat)
        {
            case NFCTypeNameFormat.NFCWellKnown:
                if (payload.Type.ToUtf8String() == "T")
                    return NdefRecordEncoding.TextMimeType;
                break;
            case NFCTypeNameFormat.Media:
                return payload.Type.ToUtf8String();
        }
        return null;
    }

    /// <summary>
    /// Returns Ndef payload into URI
    /// </summary>
    /// <param name="payload"><see cref="NFCNdefPayload"/></param>
    /// <returns><see cref="Uri"/></returns>
    public static Uri? ToUri(this NFCNdefPayload payload)
    {
        switch (payload.TypeNameFormat)
        {
            case NFCTypeNameFormat.NFCWellKnown:
                if (payload.Type.ToUtf8String() == "U")
                {
                    var uri = payload.Payload?.ParseWktUri();
                    return uri;
                }
                break;
            case NFCTypeNameFormat.AbsoluteUri:
            case NFCTypeNameFormat.Media:
                if (NdefRecordEncoding.TryDecodeUtf8(payload.Payload?.ToByteArray(), out var content) &&
                    Uri.TryCreate(content, UriKind.RelativeOrAbsolute, out var result))
                {
                    return result;
                }

                break;
        }
        return null;
    }

    private static string ToUtf8String(this NSData? data)
    {
        if (!NdefRecordEncoding.TryDecodeUtf8(data?.ToByteArray(), out var value))
            return string.Empty;

        return value;
    }

    /// <summary>
    /// Returns complete URI of TNF_WELL_KNOWN, RTD_URI records.
    /// </summary>
    /// <returns><see cref="Uri"/></returns>
    private static Uri? ParseWktUri(this NSData data)
    {
        if (NdefRecordEncoding.TryParseUriPayload(data.ToByteArray(), out var uri) &&
            Uri.TryCreate(uri, UriKind.RelativeOrAbsolute, out var result))
            return result;

        return null;
    }

    /// <summary>
    /// Returns an empty iOS <see cref="NFCNdefPayload"/>
    /// </summary>
    /// <returns>iOS <see cref="NFCNdefPayload"/></returns>
    internal static NFCNdefPayload EmptyPayload => new(NFCTypeNameFormat.Empty, new NSData(), new NSData(), new NSData());

    /// <summary>
    /// Transforms an array of <see cref="NFCNdefPayload"/> into an array of <see cref="NFCNdefRecord"/>
    /// </summary>
    /// <param name="records">Array of <see cref="NFCNdefPayload"/></param>
    /// <returns>Array of <see cref="NFCNdefRecord"/></returns>
    internal static NFCNdefRecord[] GetRecords(NFCNdefPayload[]? records)
    {
        if (records is null)
            return [];

        var results = new NFCNdefRecord[records.Length];
        for (var i = 0; i < records.Length; i++)
        {
            var record = records[i];
            var type = record.Type.ToUtf8String();
            var ndefRecord = new NFCNdefRecord
            {
                TypeFormat = (NFCNdefTypeFormat)record.TypeNameFormat,
                Uri = records[i].ToUri()?.ToString(),
                MimeType = records[i].ToMimeType() ?? string.Empty,
                Payload = record.Payload?.ToByteArray(),
                Type = record.Type?.ToByteArray(),
                Identifier = record.Identifier?.ToByteArray()
            };

            if (record.TypeNameFormat == NFCTypeNameFormat.NFCWellKnown &&
                type == "T" &&
                NdefRecordEncoding.TryParseTextPayload(ndefRecord.Payload, out _, out var languageCode))
            {
                ndefRecord.LanguageCode = languageCode;
            }

            if (record.TypeNameFormat == NFCTypeNameFormat.NFCExternal &&
                NdefRecordEncoding.TrySplitExternalType(type, out var externalDomain, out var externalType))
            {
                ndefRecord.ExternalDomain = externalDomain;
                ndefRecord.ExternalType = externalType;
            }

            results.SetValue(ndefRecord, i);
        }
        return results;
    }

    /// <summary>
    /// Returns NDEF payload
    /// </summary>
    /// <param name="record"><see cref="NFCNdefRecord"/></param>
    /// <returns><see cref="NFCNdefPayload"/></returns>
    internal static NFCNdefPayload? GetiOSPayload(NFCNdefRecord? record, NfcConfiguration configuration)
    {
        if (record is null || record.Payload is null)
            return null;

        NFCNdefPayload? payload = null;
        switch (record.TypeFormat)
        {
            case NFCNdefTypeFormat.WellKnown:
                var lang = record.LanguageCode;
                if (string.IsNullOrWhiteSpace(lang)) lang = configuration.DefaultLanguageCode;
                if (NdefRecordEncoding.TryCreateTextPayload(record.Payload, lang, out var payloadData))
                    payload = new NFCNdefPayload(NFCTypeNameFormat.NFCWellKnown, NSData.FromArray([(byte)'T']), new NSData(), NSData.FromArray(payloadData));
                break;
            case NFCNdefTypeFormat.Mime:
                payload = new NFCNdefPayload(NFCTypeNameFormat.Media, record.MimeType, new NSData(), NSData.FromArray(record.Payload));
                break;
            case NFCNdefTypeFormat.Uri:
                if (NdefRecordEncoding.TryDecodeUtf8(record.Payload, out var uri))
                    payload = NFCNdefPayload.CreateWellKnownTypePayload(NSUrl.FromString(uri)!);
                break;
            case NFCNdefTypeFormat.External:
                if (NdefRecordEncoding.TryComposeExternalType(record.ExternalDomain, record.ExternalType, out var externalType))
                    payload = new NFCNdefPayload(NFCTypeNameFormat.NFCExternal, externalType, new NSData(), NSData.FromArray(record.Payload));
                break;
            case NFCNdefTypeFormat.Empty:
                payload = EmptyPayload;
                break;
            case NFCNdefTypeFormat.Unknown:
            case NFCNdefTypeFormat.Unchanged:
            case NFCNdefTypeFormat.Reserved:
            default:
                break;
        }
        return payload;
    }
}

/// <summary>
/// NFC Tag Extensions Class
/// </summary>
internal static class NfcNdefTagExtensions
{
    /// <summary>
    /// Get Ndef tag
    /// </summary>
    /// <param name="tag"><see cref="INFCTag"/></param>
    /// <returns><see cref="INFCNdefTag"/></returns>
    internal static INFCNdefTag? GetNdefTag(INFCTag? tag)
    {
        if (tag is null || !tag.Available)
            return null;

        INFCNdefTag? ndef;

        if (tag.Type == NFCTagType.MiFare)
            ndef = tag.AsNFCMiFareTag;
        else if (tag.Type == NFCTagType.Iso7816Compatible)
            ndef = tag.AsNFCIso7816Tag;
        else if (tag.Type == NFCTagType.Iso15693)
            ndef = tag.AsNFCIso15693Tag;
        else if (tag.Type == NFCTagType.FeliCa)
            ndef = tag.AsNFCFeliCaTag;
        else
            ndef = null;

        return ndef;
    }

    /// <summary>
    /// Returns NFC Tag identifier
    /// </summary>
    /// <param name="tag"><see cref="INFCNdefTag"/></param>
    /// <returns>Tag identifier</returns>
    internal static byte[]? GetTagIdentifier(INFCNdefTag? tag)
    {
        byte[]? identifier = null;
        if (tag is INFCMiFareTag mifareTag)
        {
            identifier = mifareTag.Identifier.ToByteArray();
        }
        else if (tag is INFCFeliCaTag felicaTag)
        {
            identifier = felicaTag.CurrentIdm.ToByteArray();
        }
        else if (tag is INFCIso15693Tag iso15693Tag)
        {
            identifier = [.. iso15693Tag.Identifier.ToByteArray().Reverse()];
        }
        else if (tag is INFCIso7816Tag iso7816Tag)
        {
            identifier = iso7816Tag.Identifier.ToByteArray();
        }
        return identifier;
    }
}
