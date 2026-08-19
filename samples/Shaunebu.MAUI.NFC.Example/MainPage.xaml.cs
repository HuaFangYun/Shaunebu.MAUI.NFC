using Shaunebu.MAUI.NFC.Maui.Sample.Models;
using Shaunebu.MAUI.NFC.Maui.Sample.Services;
using Shaunebu.MAUI.NFC.Utils;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Shaunebu.MAUI.NFC.Maui.Sample;

public partial class MainPage : ContentPage, INotifyPropertyChanged
{
    public const string MimeTypeForAndroidLaunch = "application/com.companyname.nfcsample";
    public const string MIME_TYPE = MimeTypeForAndroidLaunch;

    private const string AlertTitle = "NFC";
    private readonly INFC _nfc;
    private bool _eventsSubscribed;
    private bool _pageActive;
    private bool _nfcSupported;
    private bool _nfcAvailable;
    private bool _nfcEnabled;
    private bool _writingSupported;
    private NfcCapabilities _capabilities = new();
    private bool _isListening;
    private bool _isPublishing;
    private bool _makeReadOnly;
    private WriteRecordKind _selectedRecordKind = WriteRecordKind.Text;
    private string _operationStatus = "Ready";
    private string _lastTagSummary = "No tag scanned yet.";
    private string? _lastSafeUri;
    private string _textPayload = "Hello NFC";
    private string _textLanguageCode = "en";
    private string _uriPayload = "https://github.com/Shaunebu/Shaunebu.MAUI.NFC";
    private string _mimeType = "text/plain";
    private string _mimePayload = "Hello from a MIME NDEF record.";
    private string _externalDomain = "example.com";
    private string _externalType = "asset";
    private string _externalPayload = "asset-123";

    public MainPage(INFC nfc)
    {
        InitializeComponent();
        BindingContext = this;
        _nfc = nfc;
        RefreshNfcStatus();
        AddEvent("Ready", "Sample loaded. Tap Start listening or choose a write operation.");
    }

    public ObservableCollection<NdefRecordDisplay> Records { get; } = [];

    public ObservableCollection<SampleEventLogEntry> EventLog { get; } = [];

    public string PlatformName => DeviceInfo.Platform.ToString();

    public string AvailabilitySummary => $"Supported: {FormatBoolean(_nfcSupported)} / Status: {_capabilities.Availability}";

    public string CapabilitySummary => $"Enabled: {FormatBoolean(_nfcEnabled)} / Read: {FormatBoolean(_capabilities.CanRead)} / Write: {FormatBoolean(_capabilities.CanWrite)}";

    public string OperationStatus
    {
        get => _operationStatus;
        private set => SetProperty(ref _operationStatus, value);
    }

    public string LastTagSummary
    {
        get => _lastTagSummary;
        private set => SetProperty(ref _lastTagSummary, value);
    }

    public string TextPayload
    {
        get => _textPayload;
        set => SetProperty(ref _textPayload, value);
    }

    public string TextLanguageCode
    {
        get => _textLanguageCode;
        set => SetProperty(ref _textLanguageCode, value);
    }

    public string UriPayload
    {
        get => _uriPayload;
        set => SetProperty(ref _uriPayload, value);
    }

    public string MimeType
    {
        get => _mimeType;
        set => SetProperty(ref _mimeType, value);
    }

    public string MimePayload
    {
        get => _mimePayload;
        set => SetProperty(ref _mimePayload, value);
    }

    public string ExternalDomain
    {
        get => _externalDomain;
        set => SetProperty(ref _externalDomain, value);
    }

    public string ExternalType
    {
        get => _externalType;
        set => SetProperty(ref _externalType, value);
    }

    public string ExternalPayload
    {
        get => _externalPayload;
        set => SetProperty(ref _externalPayload, value);
    }

    public bool MakeReadOnly
    {
        get => _makeReadOnly;
        set => SetProperty(ref _makeReadOnly, value);
    }

    public bool IsTextFormVisible => _selectedRecordKind == WriteRecordKind.Text;

    public bool IsUriFormVisible => _selectedRecordKind == WriteRecordKind.Uri;

    public bool IsMimeFormVisible => _selectedRecordKind == WriteRecordKind.Mime;

    public bool IsExternalFormVisible => _selectedRecordKind == WriteRecordKind.External;

    public bool CanStartListening => _nfcSupported && _nfcAvailable && _nfcEnabled && !_isListening && !_isPublishing;

    public bool CanStartPublishing => _nfcSupported && _nfcAvailable && _nfcEnabled && _writingSupported && !_isPublishing;

    public bool CanStopSession => _isListening || _isPublishing;

    public bool CanOpenLastUri => !string.IsNullOrWhiteSpace(_lastSafeUri);

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _pageActive = true;
        SubscribeEvents();
        RefreshNfcStatus();
    }

    protected override void OnDisappearing()
    {
        StopNfcSession();
        UnsubscribeEvents();
        _pageActive = false;
        base.OnDisappearing();
    }

    protected override bool OnBackButtonPressed()
    {
        StopNfcSession();
        return base.OnBackButtonPressed();
    }

    private async void OnRefreshStatusClicked(object sender, EventArgs e)
    {
        try
        {
            RefreshNfcStatus();
            AddEvent("Status", "NFC status refreshed.");
            await ShowAlertAsync(CapabilitySummary);
        }
        catch (Exception ex)
        {
            await ReportErrorAsync("Status refresh failed", ex);
        }
    }

    private async void OnStartListeningClicked(object sender, EventArgs e)
    {
        try
        {
            await StartListeningAsync();
        }
        catch (Exception ex)
        {
            await ReportErrorAsync("Start listening failed", ex);
        }
    }

    private async void OnStopClicked(object sender, EventArgs e)
    {
        try
        {
            StopNfcSession();
            AddEvent("Stopped", "NFC session stopped.");
            await ShowAlertAsync("NFC session stopped.");
        }
        catch (Exception ex)
        {
            await ReportErrorAsync("Stop failed", ex);
        }
    }

    private async void OnStartPublishingClicked(object sender, EventArgs e)
    {
        try
        {
            await StartPublishingAsync(clearMessage: false);
        }
        catch (Exception ex)
        {
            await ReportErrorAsync("Write failed", ex);
        }
    }

    private async void OnClearTagClicked(object sender, EventArgs e)
    {
        try
        {
            await StartPublishingAsync(clearMessage: true);
        }
        catch (Exception ex)
        {
            await ReportErrorAsync("Clear failed", ex);
        }
    }

    private async void OnOpenUriClicked(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_lastSafeUri))
                return;

            var open = await ShowConfirmationAsync(AlertTitle, $"Open this URI?\n\n{_lastSafeUri}", "Open", "Cancel");
            if (open)
                await Launcher.OpenAsync(_lastSafeUri);
        }
        catch (Exception ex)
        {
            await ReportErrorAsync("Open URI failed", ex);
        }
    }

    private void OnRecordKindChanged(object sender, EventArgs e)
    {
        if (sender is not Picker picker || picker.SelectedIndex < 0)
            return;

        _selectedRecordKind = (WriteRecordKind)picker.SelectedIndex;
        OnPropertyChanged(nameof(IsTextFormVisible));
        OnPropertyChanged(nameof(IsUriFormVisible));
        OnPropertyChanged(nameof(IsMimeFormVisible));
        OnPropertyChanged(nameof(IsExternalFormVisible));
    }

    private async Task StartListeningAsync()
    {
        if (!await EnsureNfcReadyAsync(requireWriting: false))
            return;

        SubscribeEvents();
        _nfc.StartListening();
        _isListening = true;
        _isPublishing = false;
        OperationStatus = "Listening for an NFC tag";
        RaiseCommandStateChanged();
        AddEvent("Listening", "Present one NDEF tag to read.");
    }

    private async Task StartPublishingAsync(bool clearMessage)
    {
        if (!await EnsureNfcReadyAsync(requireWriting: true))
            return;

        if (clearMessage)
        {
            var clear = await ShowConfirmationAsync(AlertTitle, "Clear this NFC tag by writing an empty NDEF message?", "Clear tag", "Cancel");
            if (!clear)
                return;
        }
        else if (!SampleNdefRecordFactory.TryCreate(
                     _selectedRecordKind,
                     TextPayload,
                     TextLanguageCode,
                     UriPayload,
                     MimeType,
                     MimePayload,
                     ExternalDomain,
                     ExternalType,
                     ExternalPayload,
                     out _,
                     out var validationError))
        {
            await ShowAlertAsync(validationError);
            return;
        }

        if (!clearMessage && MakeReadOnly)
        {
            var confirmed = await ShowConfirmationAsync(
                AlertTitle,
                "Making an NFC tag read-only may be permanent and cannot necessarily be undone.\n\nContinue?",
                "Write and lock",
                "Cancel");

            if (!confirmed)
                return;
        }

        SubscribeEvents();
        if (DeviceInfo.Platform != DevicePlatform.iOS && !_isListening)
            _nfc.StartListening();

        _nfc.StartPublishing(clearMessage);
        _isPublishing = true;
        _isListening = false;
        OperationStatus = clearMessage ? "Ready to clear a tag" : $"Ready to write {_selectedRecordKind} record";
        RaiseCommandStateChanged();
        AddEvent(clearMessage ? "Clear" : "Write", "Present one writable tag.");
    }

    private async Task<bool> EnsureNfcReadyAsync(bool requireWriting)
    {
        RefreshNfcStatus();

        if (!_nfcSupported || !_nfcAvailable)
        {
            await ShowAlertAsync("NFC is not available on this device or platform.");
            return false;
        }

        if (!_nfcEnabled)
        {
            await ShowAlertAsync("NFC is disabled. Enable NFC in system settings and refresh status.");
            return false;
        }

        if (requireWriting && !_writingSupported)
        {
            await ShowAlertAsync("Writing NFC tags is not supported on this device.");
            return false;
        }

        return true;
    }

    private async void Current_OnMessageReceived(ITagInfo tagInfo)
    {
        try
        {
            await RunOnMainThreadAsync(() => HandleMessageReceivedAsync(tagInfo));
        }
        catch (Exception ex)
        {
            await ReportErrorAsync("Read handling failed", ex);
        }
    }

    private async void Current_OnTagDiscovered(ITagInfo tagInfo, bool format)
    {
        try
        {
            await RunOnMainThreadAsync(() => HandleTagDiscoveredAsync(tagInfo, format));
        }
        catch (Exception ex)
        {
            await ReportErrorAsync("Tag handling failed", ex);
        }
    }

    private async void Current_OnMessagePublished(ITagInfo tagInfo)
    {
        try
        {
            await RunOnMainThreadAsync(() => HandleMessagePublishedAsync(tagInfo));
        }
        catch (Exception ex)
        {
            await ReportErrorAsync("Published handling failed", ex);
        }
    }

    private async void Current_OnNfcStatusChanged(bool isEnabled)
    {
        try
        {
            await RunOnMainThreadAsync(() =>
            {
                _nfcEnabled = isEnabled;
                OperationStatus = isEnabled ? "NFC enabled" : "NFC disabled";
                RaiseStatusChanged();
                AddEvent("Status", isEnabled ? "NFC enabled." : "NFC disabled.");
                return Task.CompletedTask;
            });
        }
        catch (Exception ex)
        {
            await ReportErrorAsync("NFC status handling failed", ex);
        }
    }

    private async void Current_OnTagListeningStatusChanged(bool isListening)
    {
        try
        {
            await RunOnMainThreadAsync(() =>
            {
                _isListening = isListening && !_isPublishing;
                if (!isListening && !_isPublishing)
                    OperationStatus = "Ready";
                RaiseCommandStateChanged();
                AddEvent("Session", isListening ? "NFC session started." : "NFC session stopped.");
                return Task.CompletedTask;
            });
        }
        catch (Exception ex)
        {
            await ReportErrorAsync("Listening status handling failed", ex);
        }
    }

    private async void Current_OniOSReadingSessionCancelled(object? sender, EventArgs e)
    {
        try
        {
            await RunOnMainThreadAsync(() =>
            {
                _isListening = false;
                _isPublishing = false;
                OperationStatus = "iOS session cancelled or invalidated";
                RaiseCommandStateChanged();
                AddEvent("iOS", "Session cancelled or invalidated.");
                return Task.CompletedTask;
            });
        }
        catch (Exception ex)
        {
            await ReportErrorAsync("iOS cancellation handling failed", ex);
        }
    }

    private Task HandleMessageReceivedAsync(ITagInfo tagInfo)
    {
        DisplayTag(tagInfo);
        _isListening = false;
        OperationStatus = tagInfo.IsSupported ? "Tag read" : "Unsupported tag detected";
        RaiseCommandStateChanged();
        AddEvent("Read", DescribeTag(tagInfo));
        return Task.CompletedTask;
    }

    private Task HandleTagDiscoveredAsync(ITagInfo tagInfo, bool format)
    {
        AddEvent("Detected", format ? "Tag discovered for clearing." : "Tag discovered for writing.");

        if (format)
        {
            _nfc.ClearMessage(tagInfo);
            return Task.CompletedTask;
        }

        if (!SampleNdefRecordFactory.TryCreate(
                _selectedRecordKind,
                TextPayload,
                TextLanguageCode,
                UriPayload,
                MimeType,
                MimePayload,
                ExternalDomain,
                ExternalType,
                ExternalPayload,
                out var record,
                out var error) ||
            record is null)
        {
            throw new InvalidOperationException(error);
        }

        tagInfo.Records = [record];
        _nfc.PublishMessage(tagInfo, MakeReadOnly);
        return Task.CompletedTask;
    }

    private async Task HandleMessagePublishedAsync(ITagInfo tagInfo)
    {
        DisplayTag(tagInfo);
        _nfc.StopPublishing();
        _isPublishing = false;
        _isListening = false;
        MakeReadOnly = false;
        OperationStatus = tagInfo.IsEmpty ? "Tag cleared" : "Tag written";
        RaiseCommandStateChanged();
        AddEvent("Success", tagInfo.IsEmpty ? "Clear operation completed." : "Write operation completed.");
        await ShowAlertAsync(tagInfo.IsEmpty ? "Tag cleared." : "Tag written.");
    }

    private void DisplayTag(ITagInfo tagInfo)
    {
        LastTagSummary = DescribeTag(tagInfo);

        Records.Clear();
        foreach (var record in NdefRecordDisplay.FromRecords(tagInfo.Records))
        {
            Records.Add(record);
        }

        _lastSafeUri = tagInfo.Records
            .Select(record => record.Uri)
            .FirstOrDefault(SampleNdefRecordFactory.IsSafeUri);

        OnPropertyChanged(nameof(CanOpenLastUri));
    }

    private string DescribeTag(ITagInfo tagInfo)
    {
        var serial = string.IsNullOrWhiteSpace(tagInfo.SerialNumber)
            ? NFCUtils.ByteArrayToHexString(tagInfo.Identifier, ":")
            : tagInfo.SerialNumber;

        if (string.IsNullOrWhiteSpace(serial))
            serial = "not available";

        var technologies = tagInfo.GetTechnologies();
        var technologySummary = technologies.Count == 0 ? "not reported" : string.Join(", ", technologies);

        return $"Serial: {serial} | Supported: {FormatBoolean(tagInfo.IsSupported)} | Empty: {FormatBoolean(tagInfo.IsEmpty)} | Writable: {FormatBoolean(tagInfo.IsWritable)} | Formatable: {FormatBoolean(tagInfo.IsFormatable)} | Capacity: {tagInfo.Capacity} bytes | Records: {tagInfo.Records.Length} | Technologies: {technologySummary}";
    }

    private void StopNfcSession()
    {
        if (!_eventsSubscribed)
            return;

        try
        {
            _nfc.StopPublishing();
            _nfc.StopListening();
        }
        catch (Exception ex)
        {
            AddEvent("Stop error", ex.Message);
        }
        finally
        {
            _isListening = false;
            _isPublishing = false;
            OperationStatus = "Ready";
            RaiseCommandStateChanged();
        }
    }

    private void SubscribeEvents()
    {
        if (_eventsSubscribed)
            return;

        _nfc.OnMessageReceived += Current_OnMessageReceived;
        _nfc.OnMessagePublished += Current_OnMessagePublished;
        _nfc.OnTagDiscovered += Current_OnTagDiscovered;
        _nfc.OnNfcStatusChanged += Current_OnNfcStatusChanged;
        _nfc.OnTagListeningStatusChanged += Current_OnTagListeningStatusChanged;
        _nfc.OniOSReadingSessionCancelled += Current_OniOSReadingSessionCancelled;
        _eventsSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!_eventsSubscribed)
            return;

        _nfc.OnMessageReceived -= Current_OnMessageReceived;
        _nfc.OnMessagePublished -= Current_OnMessagePublished;
        _nfc.OnTagDiscovered -= Current_OnTagDiscovered;
        _nfc.OnNfcStatusChanged -= Current_OnNfcStatusChanged;
        _nfc.OnTagListeningStatusChanged -= Current_OnTagListeningStatusChanged;
        _nfc.OniOSReadingSessionCancelled -= Current_OniOSReadingSessionCancelled;
        _eventsSubscribed = false;
    }

    private void RefreshNfcStatus()
    {
        _nfcSupported = CrossNFC.IsSupported;
        _capabilities = _nfcSupported ? _nfc.GetCapabilities() : new NfcCapabilities { Availability = NfcAvailabilityStatus.NotSupported };
        _nfcAvailable = _nfcSupported && _nfc.IsAvailable;
        _nfcEnabled = _nfcSupported && _nfc.IsEnabled;
        _writingSupported = _nfcSupported && _nfc.IsWritingTagSupported;
        RaiseStatusChanged();
    }

    private void RaiseStatusChanged()
    {
        OnPropertyChanged(nameof(AvailabilitySummary));
        OnPropertyChanged(nameof(CapabilitySummary));
        RaiseCommandStateChanged();
    }

    private void RaiseCommandStateChanged()
    {
        OnPropertyChanged(nameof(CanStartListening));
        OnPropertyChanged(nameof(CanStartPublishing));
        OnPropertyChanged(nameof(CanStopSession));
    }

    private void AddEvent(string kind, string message)
    {
        if (!_pageActive && EventLog.Count > 0)
            return;

        EventLog.Insert(0, SampleEventLogEntry.Create(kind, message));

        while (EventLog.Count > 25)
        {
            EventLog.RemoveAt(EventLog.Count - 1);
        }
    }

    private Task ReportErrorAsync(string context, Exception exception)
        => RunOnMainThreadAsync(async () =>
        {
            OperationStatus = context;
            AddEvent("Failure", $"{context}: {exception.Message}");
            await ShowAlertAsync($"{context}: {exception.Message}");
        });

    private Task ShowAlertAsync(string message)
        => ShowAlertAsync(AlertTitle, message, "OK");

    private Task ShowAlertAsync(string title, string message, string cancel)
    {
#if NET10_0_OR_GREATER
        return DisplayAlertAsync(title, message, cancel);
#else
        return DisplayAlert(title, message, cancel);
#endif
    }

    private Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel)
    {
#if NET10_0_OR_GREATER
        return DisplayAlertAsync(title, message, accept, cancel);
#else
        return DisplayAlert(title, message, accept, cancel);
#endif
    }

    private static Task RunOnMainThreadAsync(Func<Task> action)
    {
        if (MainThread.IsMainThread)
            return action();

        var completion = new TaskCompletionSource();
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                await action();
                completion.SetResult();
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        });

        return completion.Task;
    }

    private static string FormatBoolean(bool value) => value ? "Yes" : "No";

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
