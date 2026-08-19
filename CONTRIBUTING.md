# Contributing to Shaunebu.MAUI.NFC

Thanks for helping improve Shaunebu.MAUI.NFC. The project is intentionally small and NDEF-first, so the best contributions are focused, platform-aware, and backed by tests or real-device notes.

## Coding Style

- Keep public API changes additive unless a major release explicitly allows breaking changes.
- Preserve nullable annotations and strict build behavior.
- Follow the existing MAUI and platform partial-class structure.
- Prefer focused shared helpers for cross-platform NDEF behavior.
- Avoid unrelated refactors in behavior fixes.

## Tests and Validation

Before opening a PR, run:

```bash
dotnet restore src/Shaunebu.MAUI.NFC.sln
dotnet build src/Shaunebu.MAUI.NFC.sln -c Release --no-restore
dotnet build src/Shaunebu.MAUI.NFC/Shaunebu.MAUI.NFC.csproj -c Release --no-restore /p:TreatWarningsAsErrors=true
dotnet test tests/Shaunebu.MAUI.NFC.Tests/Shaunebu.MAUI.NFC.Tests.csproj -c Release --no-restore
dotnet pack src/Shaunebu.MAUI.NFC/Shaunebu.MAUI.NFC.csproj -c Release --no-restore
```

If your change affects real NFC behavior, include physical device validation notes. Simulators and emulators are not enough for final NFC confidence.

## Sample Device Validation

Use `samples/Shaunebu.MAUI.NFC.Example` as the canonical manual validation app. For NFC changes, record PASS/FAIL notes for the relevant flows:

- Android: availability, enabled/disabled state, read, write, clear, read-only confirmation, repeated scans, read-after-write, tag removal, and MIME intent launch.
- iOS: availability, read, write, clear, read-only confirmation, cancellation, multiple tags, repeated sessions, and tag removal.
- Tag data: identifier/serial display, writable flag, capacity, supported/empty/formatable flags, and multiple NDEF record display.

Do not claim physical validation unless it was performed on real hardware with real NFC tags.

## Pull Requests

Please include:

- The user-visible behavior or documentation change.
- Affected platform(s): Android, iOS, or both.
- Tests added or updated.
- Manual device/tag validation, if applicable.
- Any known limitations or follow-up work.

## Issues

Please include:

- Device model and OS version.
- App target framework.
- Package version.
- Tag type or chip, if known.
- Whether the tag is empty, NDEF-formatted, writable, or read-only.
- Minimal code and platform configuration needed to reproduce.
- Exact exception text, session message, or observed behavior.

## Documentation

Documentation should be practical and consumer-facing. Avoid advertising features that are not implemented, and call out platform differences when they affect how an app should be built.
