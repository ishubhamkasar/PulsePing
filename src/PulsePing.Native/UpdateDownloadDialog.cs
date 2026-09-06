// Copyright (C) 2026 Shubham Kasar
// SPDX-License-Identifier: GPL-3.0-only
namespace PulsePingNative;

internal sealed class UpdateDownloadDialog : Form
{
    private readonly CancellationTokenSource _cancellation = new();
    private readonly ProgressBar _bar = new() { Dock = DockStyle.Top, Height = 22, Maximum = 100 };
    private readonly Label _status = new() { AutoSize = true, Dock = DockStyle.Top, Margin = new Padding(0, 10, 0, 18) };
    private readonly Button _action = new() { Text = "Cancel", AutoSize = true };
    private readonly Button _later = new() { Text = "Later", AutoSize = true, Visible = false };
    private readonly Label _heading;
    private bool _downloading;
    private DownloadedUpdate? _download;
    public bool InstallRequested { get; private set; }

    public UpdateDownloadDialog(UpdateCheckResult release, AppTheme theme)
    {
        Text = "PulsePing update";
        AutoScaleDimensions = new SizeF(96, 96);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = Typography.Interface(10f);
        ClientSize = new Size(510, 255);
        MinimumSize = new Size(440, 290);
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false; MinimizeBox = false; ShowInTaskbar = false;
        BackColor = theme.Background; ForeColor = theme.Foreground;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24), ColumnCount = 1, RowCount = 4 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _heading = new Label { Text = $"Downloading PulsePing {release.LatestVersionText}",
            AutoSize = true, Dock = DockStyle.Top, Font = Typography.Interface(13f, FontStyle.Bold), Margin = new Padding(0, 0, 0, 18) };
        layout.Controls.Add(_heading, 0, 0);
        layout.Controls.Add(_bar, 0, 1);
        layout.Controls.Add(_status, 0, 2);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft };
        buttons.Controls.Add(_action); buttons.Controls.Add(_later);
        layout.Controls.Add(buttons, 0, 3);
        Controls.Add(layout);
        _action.Click += (_, _) =>
        {
            if (_downloading) { _cancellation.Cancel(); _action.Enabled = false; _status.Text = "Cancelling…"; return; }
            if (_download is null) { Close(); return; }
            if (MessageBox.Show(this, "Install the downloaded update and restart PulsePing now?\n\nActive ping monitors will stop when the app restarts.",
                    "Install PulsePing update", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            try
            {
                UpdateInstaller.Start(_download);
                InstallRequested = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "The update could not be started. Your current app is still running.\n\n" + ex.Message,
                    "PulsePing update", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        _later.Click += (_, _) => Close();
        FormClosing += (_, e) => { if (_downloading) { e.Cancel = true; _cancellation.Cancel(); } };
        Shown += async (_, _) =>
        {
            _downloading = true;
            _status.Text = "Connecting to the official release…";
            try
            {
                var progress = new Progress<DownloadProgress>(p =>
                {
                    if (IsDisposed || Disposing || !_downloading) return;
                    _bar.Value = p.Percent;
                    _status.Text = $"{p.Percent}%  •  {p.Received / 1048576d:0.0} MB / {p.Total / 1048576d:0.0} MB";
                });
                _download = await UpdateChecker.DownloadAsync(release, progress, _cancellation.Token);
                _bar.Value = 100;
                _heading.Text = $"PulsePing {release.LatestVersionText} is ready";
                _status.Text = "Download complete and verified.\nInstall now, or choose Later to keep working.";
                _action.Text = "Install and restart";
                _later.Visible = true;
            }
            catch (OperationCanceledException) when (_cancellation.IsCancellationRequested)
            { _status.Text = "Download cancelled."; _action.Text = "Close"; }
            catch (Exception ex) { _status.Text = "Download failed: " + ex.Message; _action.Text = "Close"; }
            finally { _downloading = false; _action.Enabled = true; }
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cancellation.Dispose();
            // Keep downloaded files only while the installation helper needs them.
            if (!InstallRequested && _download is not null)
            { try { File.Delete(_download.Path); Directory.Delete(Path.GetDirectoryName(_download.Path)!); } catch { } }
        }
        base.Dispose(disposing);
    }
}
