## Changelog

### 1.1.0
- Update the package for .NET 9 and .NET 10 MAUI Android/iOS targets while retaining .NET 9 package assets.
- Enable package validation with API compatibility checked against 1.0.4.
- Improve NDEF text parsing, iOS text payload writing, encoded-size capacity checks, and external type handling.
- Harden current Android/iOS read, write, cleanup, and multiple-tag behavior without changing the public event API.
- Add backward-compatible capability reporting through `CrossNFC.GetCapabilities()` and `INFC.GetCapabilities()`.
- Add configurable event dispatch through `NfcOptions.EventDispatchMode`, including optional main-thread dispatch and subscriber exception isolation.
- Add optional redacted diagnostics through `NfcOptions.LoggerFactory` and stable `NfcLogEventIds`.
- Add tag technology metadata helpers and preserve raw NDEF type/id metadata when reading records.
- Expand automated tests and package smoke coverage.
- Rewrite README documentation and polish NuGet metadata for the production-readiness release.
- Modernize the existing MAUI sample into the canonical reference app with Text, URI, MIME, External, clear, read-only, multiple-record display, event history, and warning-free builds.

### 1.0.0
- Update .NET Maui support
- Remove Xamarin support

### 0.1.26
- GitHub #113 : [iOS] Fix Null reference Exception when tag contains no records
- GitHub #97,119 : Add Maui Support (PR #116)

### 0.1.23
- GitHub #107 : [iOS] Fix Null Reference Exception on iOS 15.4 (PR #110)

### 0.1.22
- GitHub #95 : [iOS] Add write support for Mifare Classic 1K tags (Legacy mode) (PR #96)
- GitHub #89 : [iOS] Fix native class hasn't been loaded (CoreNFC.NFCNdefReaderSession)
- GitHub #88 : [iOS] Raise OniOSReadingSessionCancelled when session times out (PR #93)

### 0.1.21
- GitHub #90 : [ANDROID] Sdk 31 "FLAG_IMMUTABLE" issue (PR #91)
- GitHub #84 : [iOS] Fix Unhandled null reference exception (PR #86)
- GitHub #82 : Fix typo in sample (PR #85)

### 0.1.20
- [iOS] Support for Mifare Desfire EV3 tags (PR #75)
- GitHub #44 : [iOS] Issues Reading Mifare Classic 1k on iOS (PR #73)
- GitHub #58 : [ANDROID] CrossNFC.Current.IsEnabled status not changing (PR #76)

### 0.1.19
- GitHub #41,62,64 : [iOS] Add support for ISO 15693 tags (PR #61)
- GitHub #52 : [ANDROID, iOS] Language Code Support (PR #47)

### 0.1.18
- [Android] Fixing some issues with NFC listener on Android 10 (PR #49)
- GitHub #40 : [iOS] NFC Tag not reading on iOS unless I have a message on the tag (PR #48)
- GitHub #45 : [ANDROID, iOS] Expose the capacity of the ndef tag (PR #46)

### 0.1.17
- Update Shaunebu.MAUI.NFC.csproj
- Add [sourcelink](https://github.com/dotnet/sourcelink) support
- GitHub #28 : [iOS] Change default UI messages

### 0.1.16
- GitHub #29 : [ANDROID] App sample - Write functionality needs fixing (PR #30)

### 0.1.15
- [Android] Add event listener for NFC status.

### 0.1.14 
- [iOS] Fixed OniOSReadingSessionCancelled calls.

### 0.1.13
- GitHub #4 : [iOS] Support for writing NFC Tags.
- GitHub #8 : Make Read Only.

### 0.1.11
- GitHub #5 : [iOS] Application crash when NFC tag is empty.
- [iOS] Adding event when user cancels NFC reading session.
- Minor changes.

### 0.1.10
- GitHub #1 : [ANDROID] Add support nfc tag id (serial number).

### 0.1.9
- GitHub #1 : [ANDROID] Add support nfc tag id (serial number).

### 0.1.8
- First public release.

### 0.1.0
- Add read/write support for Android.
- Add shared interfaces and classes.
