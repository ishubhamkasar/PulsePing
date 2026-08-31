# Code Signing Policy

## Purpose

This policy defines how official PulsePing binaries are built, reviewed, approved, and signed. Free code signing is planned to be provided by SignPath.io, with a certificate issued by the SignPath Foundation, subject to project acceptance.

## Source and licensing

- Official releases must be reproducibly derived from this public source repository.
- The project is licensed under the GNU General Public License v3.0.
- Proprietary or unpublished production source components are not permitted.
- Generated build folders, recovered artifacts, certificates, private keys, and local secrets are excluded from the repository.

## Build process

- CI builds run on GitHub-hosted Windows runners.
- The workflow restores the declared .NET SDK and publishes the checked-in project source.
- The resulting standalone executable is initially treated as unsigned.
- Only CI artifacts produced from the protected default branch or an approved release tag may enter the signing process.

## Review and approval

- Maintainer and code reviewer: [Shubham Kasar](https://github.com/ishubhamkasar).
- Signing approver: [Shubham Kasar](https://github.com/ishubhamkasar).
- A signing request requires a successful CI build and manual approval.
- The approver verifies the source revision, workflow result, artifact name, and intended version before signing.
- Signing is denied for unreviewed forks, local binaries, failed builds, or artifacts not traceable to a repository revision.

## Privacy and external communication

PulsePing has no telemetry, analytics, advertising, account system, update agent, or cloud backend. It communicates only by sending ICMP echo requests to targets explicitly entered by the user after the user starts monitoring. It does not scan IP ranges, discover subnets, enumerate addresses, or contact a maintainer-controlled service.

## Key protection

No signing private key is stored in this repository, its GitHub secrets, developer machines, or build artifacts. Signing keys remain under the signing service's control.

## Release identification

Official releases are identified by a versioned Git tag and accompanied by release notes and a SHA-256 checksum. The website must distinguish signed official releases from unsigned CI artifacts.

## Incident response

If a signing or supply-chain compromise is suspected, signing is paused, affected releases are removed from distribution, the signing provider is notified, and users are informed through the repository's security advisory and release channels.
