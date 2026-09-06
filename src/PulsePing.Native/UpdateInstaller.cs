// Copyright (C) 2026 Shubham Kasar
// SPDX-License-Identifier: GPL-3.0-only
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace PulsePingNative;

internal sealed record InstallRequest(string Target, string OriginalHash, string UpdateHash, string Version,
    int ParentId, long ParentStartedUtc);

internal static class UpdateInstaller
{
    internal static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    public static void Start(DownloadedUpdate update)
    {
        string target = Environment.ProcessPath ?? throw new IOException("Cannot locate PulsePing.");
        string directory = Path.GetDirectoryName(update.Path)!;
        if (!Hash(update.Path).Equals(update.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The downloaded update changed. Please download it again.");
        // Check write access before stopping a monitoring session.
        string probe = Path.Combine(Path.GetDirectoryName(target)!, ".pulseping-write-" + Guid.NewGuid().ToString("N"));
        using (new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose)) { }
        using var parent = Process.GetCurrentProcess();
        var request = new InstallRequest(target, Hash(target), update.Sha256, update.Version, parent.Id, parent.StartTime.ToUniversalTime().Ticks);
        string helper = Path.Combine(directory, "installer.exe");
        File.Copy(target, helper, true);
        File.WriteAllText(Path.Combine(directory, "install.json"), JsonSerializer.Serialize(request));
        var start = new ProcessStartInfo(helper) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = directory };
        start.ArgumentList.Add("--apply-update");
        if (Process.Start(start) is null) throw new IOException("Could not start the update installer.");
    }

    public static void Run()
    {
        string directory = Path.GetDirectoryName(Environment.ProcessPath!)!;
        string candidate = Path.Combine(directory, "update.exe");
        string? staged = null;
        try
        {
            var request = JsonSerializer.Deserialize<InstallRequest>(File.ReadAllText(Path.Combine(directory, "install.json")))
                ?? throw new InvalidDataException("Missing update instructions.");
            string target = Path.GetFullPath(request.Target);
            if (!Path.IsPathFullyQualified(request.Target) || !target.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                target.Equals(Environment.ProcessPath, StringComparison.OrdinalIgnoreCase) ||
                target.Equals(candidate, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Invalid installation target.");
            try
            {
                using var parent = Process.GetProcessById(request.ParentId);
                if (parent.StartTime.ToUniversalTime().Ticks == request.ParentStartedUtc && !parent.WaitForExit(60000))
                    throw new IOException("PulsePing is still running. Close it and try installing again.");
            }
            catch (ArgumentException) { /* Parent already exited. */ }
            string backup = target + ".previous-" + Guid.NewGuid().ToString("N");
            staged = target + ".update-" + Guid.NewGuid().ToString("N");
            File.Copy(candidate, staged, false);
            var identity = FileVersionInfo.GetVersionInfo(staged);
            if (identity.ProductName != "PulsePing Network Monitor" ||
                !Version.TryParse(identity.FileVersion, out var actualVersion) ||
                !Version.TryParse(request.Version, out var expectedVersion) ||
                actualVersion.Major != expectedVersion.Major || actualVersion.Minor != expectedVersion.Minor ||
                actualVersion.Build != expectedVersion.Build)
                throw new InvalidDataException("The downloaded executable does not match the PulsePing release version.");
            if (!Hash(staged).Equals(request.UpdateHash, StringComparison.OrdinalIgnoreCase) ||
                !Hash(target).Equals(request.OriginalHash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("An application file changed during installation. The existing app was preserved.");
            // Stage on the destination volume, then replace atomically and retain a rollback copy.
            File.Replace(staged, target, backup);
            staged = null;
            try
            {
                if (Process.Start(new ProcessStartInfo(target) { UseShellExecute = true, WorkingDirectory = Path.GetDirectoryName(target)! }) is null)
                    throw new IOException("The updated app could not be started.");
            }
            catch
            {
                File.Replace(backup, target, null);
                throw;
            }
            try { File.Delete(backup); File.Delete(candidate); File.Delete(Path.Combine(directory, "install.json")); } catch { }
        }
        catch (Exception ex)
        {
            MessageBox.Show("PulsePing could not finish the update.\n\n" + ex.Message +
                "\n\nYour application has not been removed. Reopen it and try again.",
                "PulsePing update", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally { if (staged is not null) { try { File.Delete(staged); } catch { } } }
    }
}
