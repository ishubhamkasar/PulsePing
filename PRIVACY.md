# Privacy Policy

Effective date: September 6, 2026

PulsePing is a local Windows network-monitoring application created and developed by Shubham Kasar.

## Data collection

PulsePing does not collect, sell, transmit, or share personal information. It contains no telemetry, analytics, advertising, user accounts, or cloud service.

## Network communication

PulsePing sends ICMP echo requests only to a hostname or IP address explicitly entered by the user and only after the user starts monitoring that target. Replies, timing information, and calculated statistics are processed locally on the user's computer.

PulsePing does not perform IP-range scanning, subnet discovery, address enumeration, or automatic network discovery.

## Update checks

PulsePing can check the official GitHub Releases API for newer versions. Manual checks occur only when the user selects **Check for updates**. Automatic checks are disabled until the user explicitly enables them and then run at most once every 24 hours while PulsePing starts.

An update request sends the installed PulsePing version as part of its HTTPS user-agent and exposes ordinary connection metadata, such as the public IP address, to GitHub. It never includes monitored hostnames, IP addresses, ping history, imported host lists, or other application content. PulsePing does not automatically download or install releases; it only offers to open the official GitHub release page.

## Local data

The automatic-update preference and last successful check time are stored in the user's local application-data folder. Any files the user chooses to import or export also remain on the user's computer. PulsePing does not upload them.

## Operating-system behavior

Windows and the destination network may independently record ordinary network metadata such as source and destination addresses. That processing is controlled by those systems, not by PulsePing.

## Changes

Material privacy changes will be documented in the repository and release notes.
