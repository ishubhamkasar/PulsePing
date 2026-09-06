// PulsePing - native Windows ICMP monitor
// Copyright (C) 2026 Shubham Kasar
// SPDX-License-Identifier: GPL-3.0-only
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PulsePingNative;

internal sealed record UpdateCheckResult(Version CurrentVersion, Version LatestVersion,
    string LatestVersionText, Uri ReleasePage, GitHubReleaseAsset? Asset)
{
    public bool IsUpdateAvailable => LatestVersion > CurrentVersion;
}

internal sealed class GitHubReleaseDocument
{
    [JsonPropertyName("tag_name")] public string TagName { get; init; } = "";
    [JsonPropertyName("html_url")] public string HtmlUrl { get; init; } = "";
    [JsonPropertyName("assets")] public GitHubReleaseAsset[] Assets { get; init; } = [];
}

internal sealed class GitHubReleaseAsset
{
    [JsonPropertyName("name")] public string Name { get; init; } = "";
    [JsonPropertyName("size")] public long Size { get; init; }
    [JsonPropertyName("digest")] public string? Digest { get; init; }
    [JsonPropertyName("browser_download_url")] public string DownloadUrl { get; init; } = "";
}

internal sealed record DownloadProgress(long Received, long Total)
{
    public int Percent => Total > 0 ? (int)Math.Min(100, Received * 100 / Total) : 0;
}
internal sealed record DownloadedUpdate(string Path, string Sha256, string Version);

internal static class UpdateChecker
{
    internal const string ReleasePrefix = "https://github.com/ishubhamkasar/PulsePing/releases/";
    private static readonly HttpClient Client = CreateClient();
    public static Version CurrentVersion => typeof(UpdateChecker).Assembly.GetName().Version ?? new Version(1, 0, 0, 0);
    public static string CurrentVersionText => FormatVersion(CurrentVersion);

    internal static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PulsePing", CurrentVersionText));
        return client;
    }

    public static async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        using var request = new HttpRequestMessage(HttpMethod.Get,
            "https://api.github.com/repos/ishubhamkasar/PulsePing/releases/latest");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        using var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
        var release = await JsonSerializer.DeserializeAsync<GitHubReleaseDocument>(stream, cancellationToken: timeout.Token).ConfigureAwait(false)
            ?? throw new InvalidDataException("GitHub returned an empty release response.");
        return ParseRelease(release);
    }

    internal static UpdateCheckResult ParseRelease(GitHubReleaseDocument release)
    {
        string tag = release.TagName.TrimStart('v', 'V');
        if (!Version.TryParse(tag, out var version)) throw new InvalidDataException("Invalid release version.");
        version = new Version(version.Major, version.Minor, Math.Max(0, version.Build), Math.Max(0, version.Revision));
        if (!IsOfficialUrl(release.HtmlUrl, "tag/")) throw new InvalidDataException("Invalid release page.");
        var candidates = release.Assets.Where(a => a.Name.StartsWith("PulsePing", StringComparison.OrdinalIgnoreCase) &&
            a.Name.EndsWith("win-x64.exe", StringComparison.OrdinalIgnoreCase) &&
            IsOfficialUrl(a.DownloadUrl, "download/")).ToArray();
        // Ambiguous releases must be corrected by the publisher, never guessed by the updater.
        var asset = candidates.Length == 1 ? candidates[0] : null;
        return new(CurrentVersion, version, FormatVersion(version), new Uri(release.HtmlUrl), asset);
    }

    internal static bool IsOfficialUrl(string value, string section) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == "https" &&
        uri.IsDefaultPort && string.IsNullOrEmpty(uri.UserInfo) &&
        uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase) &&
        uri.AbsolutePath.StartsWith("/ishubhamkasar/PulsePing/releases/" + section, StringComparison.Ordinal);

    public static Task<DownloadedUpdate> DownloadAsync(UpdateCheckResult release, IProgress<DownloadProgress> progress,
        CancellationToken token) => DownloadAsync(release, progress, token, Client,
            System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PulsePing", "Updates"));

    internal static async Task<DownloadedUpdate> DownloadAsync(UpdateCheckResult release, IProgress<DownloadProgress> progress,
        CancellationToken token, HttpClient client, string root)
    {
        var asset = release.Asset ?? throw new InvalidDataException("This release has no unique Windows x64 executable. Please try again after the publisher finishes uploading it.");
        if (!IsOfficialUrl(asset.DownloadUrl, "download/") || asset.Size <= 0 || asset.Size > 512L * 1024 * 1024)
            throw new InvalidDataException("Invalid update download metadata.");
        if (asset.Digest is null || !System.Text.RegularExpressions.Regex.IsMatch(asset.Digest, "^sha256:[0-9a-fA-F]{64}$"))
            throw new InvalidDataException("This release has no SHA-256 checksum. It cannot be installed safely.");
        string expected = asset.Digest[7..];
        string directory = System.IO.Path.Combine(root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string partial = System.IO.Path.Combine(directory, "download.partial");
        string complete = System.IO.Path.Combine(directory, "update.exe");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromMinutes(30));
        try
        {
            using var response = await client.GetAsync(asset.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength is long length && length != asset.Size)
                throw new InvalidDataException("The download size does not match the release.");
            await using var input = await response.Content.ReadAsStreamAsync(timeout.Token).ConfigureAwait(false);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            long received = 0;
            var reportClock = System.Diagnostics.Stopwatch.StartNew();
            await using (var output = new FileStream(partial, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
            {
                byte[] buffer = new byte[81920];
                while (true)
                {
                    int read = await input.ReadAsync(buffer, timeout.Token).AsTask().WaitAsync(TimeSpan.FromSeconds(30), timeout.Token).ConfigureAwait(false);
                    if (read == 0) break;
                    received += read;
                    if (received > asset.Size) throw new InvalidDataException("The download exceeded the expected size.");
                    hash.AppendData(buffer, 0, read);
                    await output.WriteAsync(buffer.AsMemory(0, read), timeout.Token).ConfigureAwait(false);
                    if (reportClock.ElapsedMilliseconds >= 100) { progress.Report(new(received, asset.Size)); reportClock.Restart(); }
                }
            }
            if (received != asset.Size || !Convert.ToHexString(hash.GetHashAndReset()).Equals(expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Update verification failed. The downloaded file was discarded; please try again.");
            token.ThrowIfCancellationRequested();
            File.Move(partial, complete);
            progress.Report(new(received, asset.Size));
            return new(complete, expected, release.LatestVersionText);
        }
        catch
        {
            try { File.Delete(partial); File.Delete(complete); Directory.Delete(directory); } catch { }
            throw;
        }
    }

    private static string FormatVersion(Version version) => $"{version.Major}.{version.Minor}.{Math.Max(0, version.Build)}";
}
