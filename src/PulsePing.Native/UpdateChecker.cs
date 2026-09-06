// PulsePing - native Windows ICMP monitor
// Copyright (C) 2026 Shubham Kasar
// SPDX-License-Identifier: GPL-3.0-only

using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PulsePingNative;

internal sealed record UpdateCheckResult(
    Version CurrentVersion,
    Version LatestVersion,
    string LatestVersionText,
    Uri ReleasePage)
{
    public bool IsUpdateAvailable => LatestVersion > CurrentVersion;
}

internal sealed class GitHubReleaseDocument
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; init; } = string.Empty;

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; init; } = string.Empty;
}

internal static class UpdateChecker
{
    private static readonly Uri LatestReleaseEndpoint =
        new("https://api.github.com/repos/ishubhamkasar/PulsePing/releases/latest");

    private static readonly HttpClient Client = CreateClient();

    public static Version CurrentVersion =>
        typeof(UpdateChecker).Assembly.GetName().Version ?? new Version(1, 0, 0, 0);

    public static string CurrentVersionText => FormatVersion(CurrentVersion);

    public static async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseEndpoint);
        using HttpResponseMessage response = await Client.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        GitHubReleaseDocument release = await JsonSerializer.DeserializeAsync<GitHubReleaseDocument>(
            responseStream, cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("GitHub returned an empty release response.");

        Version latestVersion = ParseVersionTag(release.TagName)
            ?? throw new InvalidDataException("The latest GitHub release has an invalid version tag.");

        if (!Uri.TryCreate(release.HtmlUrl, UriKind.Absolute, out Uri? releasePage) ||
            releasePage.Scheme != Uri.UriSchemeHttps ||
            !releasePage.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("GitHub returned an invalid release page address.");
        }

        return new UpdateCheckResult(CurrentVersion, latestVersion, FormatVersion(latestVersion), releasePage);
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("PulsePing", CurrentVersionText));
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        return client;
    }

    private static Version? ParseVersionTag(string tag)
    {
        string normalized = tag.Trim().TrimStart('v', 'V');
        int suffix = normalized.IndexOfAny(['-', '+']);
        if (suffix >= 0) normalized = normalized[..suffix];
        return Version.TryParse(normalized, out Version? version) ? version : null;
    }

    private static string FormatVersion(Version version)
    {
        int build = Math.Max(0, version.Build);
        return $"{version.Major}.{version.Minor}.{build}";
    }
}
