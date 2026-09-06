# PulsePing

PulsePing is a native Windows application for monitoring the reachability, latency, and packet-loss behavior of individual network hosts. It was created and developed by Shubham Kasar.

PulsePing is intentionally focused: it sends ICMP echo requests only to hostnames or IP addresses explicitly entered by the user. It does not contain an IP-range scanner, subnet discovery, or automatic network-discovery capability.

## Features

- Monitor several explicitly entered hosts at the same time.
- Animated response stream with recent latency history.
- Average latency, packet-loss, and interval statistics.
- Pause and resume each monitor independently.
- Open a host in an independent pop-out window.
- Pin a pop-out window above other applications.
- Check GitHub Releases manually or through an optional once-daily startup check.
- Light and dark themes.
- Single-file, self-contained Windows x64 release.

## Requirements

- Windows 10 or Windows 11, x64.
- The .NET 10 SDK is required only when building from source.

## Build from source

From PowerShell at the repository root:

```powershell
pwsh ./scripts/Build-Release.ps1
```

The unsigned standalone executable is written to `artifacts/PulsePing1.0.exe`. It is self-contained, so end users do not need to install .NET separately.

GitHub Actions also builds the same unsigned artifact for every push and pull request. Official website releases will be code-signed only through the documented release process.

## Privacy and network behavior

PulsePing has no telemetry, advertising, analytics, account system, or cloud backend. ICMP traffic is initiated only after the user enters a target and starts a monitor. Update checks use GitHub Releases only when requested manually or explicitly enabled by the user; they never include monitored addresses or ping history. See [PRIVACY.md](PRIVACY.md) for details.

## Security

Please report security issues using the process in [SECURITY.md](SECURITY.md). Do not publish exploitable details in a public issue.

## Code signing policy

PulsePing is applying for **free code signing provided by SignPath.io, certificate by SignPath Foundation**. Approval is pending, so the current downloadable Windows binary remains explicitly marked as unsigned. Every future signing request must originate from the public GitHub Actions build and receive manual approval. See the complete [code signing policy](CODE_SIGNING_POLICY.md).

## Contributing

Contributions are welcome. Please read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request.

## License

PulsePing is free and open-source software licensed under the [GNU General Public License v3.0](LICENSE).

Copyright (c) 2026 Shubham Kasar.
