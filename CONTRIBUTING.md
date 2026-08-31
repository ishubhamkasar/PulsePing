# Contributing to PulsePing

Thank you for helping improve PulsePing.

## Before contributing

1. Search existing issues before opening a new one.
2. Keep each pull request focused on one change.
3. Explain the user-facing behavior and the reason for the change.
4. Test on a supported Windows 10 or Windows 11 x64 system.

## Build and validation

Run:

```powershell
pwsh ./scripts/Build-Release.ps1
```

Confirm that `artifacts/PulsePing1.0.exe` starts successfully and that existing monitoring behavior still works.

## Project boundaries

PulsePing accepts only targets explicitly entered by the user. Contributions must not add IP-range scanning, subnet discovery, automatic address enumeration, covert telemetry, credential collection, or security-control bypasses.

## License

By submitting a contribution, you agree that it is licensed under the GNU General Public License v3.0.
