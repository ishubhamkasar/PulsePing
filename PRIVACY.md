# Privacy Policy

Effective date: September 6, 2026

PulsePing is a local Windows network-monitoring application created and developed by Shubham Kasar.

## Data collection

PulsePing contains no telemetry, analytics, advertising, user accounts, or cloud storage. Update checks expose ordinary connection metadata to GitHub as described below.

## Network communication

PulsePing sends ICMP echo requests only to a hostname or IP address explicitly entered by the user and only after the user starts monitoring that target. Replies, timing information, and calculated statistics are processed locally on the user's computer.

PulsePing does not perform IP-range scanning, subnet discovery, address enumeration, or automatic network discovery.

## Update checks

PulsePing checks the official GitHub Releases API in the background every time the application opens. It shows a popup only when a newer version is available; startup checks remain silent when the app is current or the service cannot be reached. Manual checks are also available through **Check for updates**.

An update request sends the installed PulsePing version as part of its HTTPS user-agent and exposes ordinary connection metadata, such as the public IP address, to GitHub. It never includes monitored hostnames, IP addresses, ping history, imported host lists, or other application content. After the user approves a download, PulsePing downloads the Windows executable from the official GitHub release and its asset-hosting service, showing progress inside the app. It verifies the release SHA-256 checksum before offering installation. Installation and restart require a separate user action.

## Local data

Update checks do not store preferences or check history. Approved downloads and temporary installation files are stored under the user's local application-data folder in PulsePing/Updates. Cancelled or failed downloads are discarded. Installation uses a temporary backup of the existing executable and removes it after successfully launching the replacement; the temporary helper may remain in the update folder. Any files the user chooses to import or export also remain on the user's computer. PulsePing does not upload them.

## Operating-system behavior

Windows and the destination network may independently record ordinary network metadata such as source and destination addresses. That processing is controlled by those systems, not by PulsePing.

## Changes

Material privacy changes will be documented in the repository and release notes.
