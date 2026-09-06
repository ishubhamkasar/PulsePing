// PulsePing - native Windows ICMP monitor
// Copyright (C) 2026 Shubham Kasar
// SPDX-License-Identifier: GPL-3.0-only

using System.Drawing.Drawing2D;
using System.Net.NetworkInformation;
using System.ComponentModel;
using System.Text.Json;

namespace PulsePingNative;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

internal sealed record AppTheme(
    Color Background,
    Color Toolbar,
    Color Card,
    Color Input,
    Color Foreground,
    Color Muted,
    Color Border,
    Color Accent,
    Color AccentHover,
    Color Online,
    Color OnlineCard,
    Color Offline,
    Color OfflineCard,
    Color Separator,
    Color Network);

internal static class Themes
{
    public static readonly AppTheme Light = new(
        Color.FromArgb(240, 244, 250), Color.White,
        Color.FromArgb(251, 252, 255), Color.White,
        Color.FromArgb(12, 25, 43), Color.FromArgb(102, 117, 139),
        Color.FromArgb(203, 216, 235), Color.FromArgb(10, 132, 255),
        Color.FromArgb(0, 112, 224), Color.FromArgb(15, 169, 104),
        Color.FromArgb(221, 248, 235), Color.FromArgb(229, 72, 93),
        Color.FromArgb(253, 232, 236),
        Color.FromArgb(220, 230, 243), Color.FromArgb(93, 190, 245));

    public static readonly AppTheme Dark = new(
        Color.FromArgb(10, 14, 22), Color.FromArgb(18, 24, 35),
        Color.FromArgb(22, 29, 42), Color.FromArgb(13, 20, 32),
        Color.FromArgb(247, 249, 253), Color.FromArgb(157, 170, 192),
        Color.FromArgb(46, 59, 80), Color.FromArgb(79, 140, 255),
        Color.FromArgb(112, 165, 255), Color.FromArgb(52, 227, 154),
        Color.FromArgb(11, 48, 37), Color.FromArgb(255, 98, 116),
        Color.FromArgb(58, 21, 29),
        Color.FromArgb(42, 55, 74), Color.FromArgb(85, 214, 255));
}

internal static class Typography
{
    // Windows' variable UI family keeps every surface crisp at fractional DPI.
    public static Font Royal(float size, FontStyle style = FontStyle.Regular) =>
        new("Segoe UI Variable Display", size, style, GraphicsUnit.Point);

    public static Font Interface(float size, FontStyle style = FontStyle.Regular) =>
        new("Segoe UI Variable Text", size, style, GraphicsUnit.Point);
}

internal static class DrawingTools
{
    public static GraphicsPath Rounded(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        float diameter = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));
        var arc = new RectangleF(rect.X, rect.Y, diameter, diameter);
        path.AddArc(arc, 180, 90);
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = rect.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = rect.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static Color Mix(Color a, Color b, float amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromArgb(
            (int)(a.R + (b.R - a.R) * amount),
            (int)(a.G + (b.G - a.G) * amount),
            (int)(a.B + (b.B - a.B) * amount));
    }
}

internal sealed class PillButton : Button
{
    private bool _hovered;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color FillColor { get; set; } = Color.White;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color HoverColor { get; set; } = Color.Gainsboro;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color TextColor { get; set; } = Color.Black;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Glyph { get; set; } = "";

    public PillButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        UseVisualStyleBackColor = false;
        Cursor = Cursors.Hand;
        TabStop = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
    }

    protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        var rect = new RectangleF(1, 1, Width - 2, Height - 2);
        using var path = DrawingTools.Rounded(rect, Height / 2f);
        using var brush = new SolidBrush(_hovered ? HoverColor : FillColor);
        e.Graphics.FillPath(brush, path);
        using var border = new Pen(DrawingTools.Mix(FillColor, TextColor, .12f));
        e.Graphics.DrawPath(border, path);

        string label = string.IsNullOrWhiteSpace(Glyph) ? Text : $"{Glyph}  {Text}".TrimEnd();
        TextRenderer.DrawText(e.Graphics, label, Font, ClientRectangle, TextColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
    }
}

internal sealed class BrandMark : Control
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color Accent { get; set; } = Color.DodgerBlue;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public float AnimationPhase { get; set; }

    public BrandMark()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = new RectangleF(1, 1, Width - 3, Height - 3);
        using var path = DrawingTools.Rounded(rect, 9);
        using var fill = new SolidBrush(Accent);
        e.Graphics.FillPath(fill, path);
        using var pulse = new Pen(Color.White, 1.8f)
        { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        float y = Height / 2f;
        PointF[] points =
        [
            new(7f, y), new(11f, y), new(14f, y - 5), new(18f, y + 5),
            new(22f, y - 2), new(25f, y), new(Width - 7f, y)
        ];
        e.Graphics.DrawLines(pulse, points);

        // A restrained packet highlight travels across the pulse trace.
        float packetX = 7f + (Width - 14f) * AnimationPhase;
        float packetY = y + MathF.Sin(AnimationPhase * MathF.PI * 2f) * 1.5f;
        using var glow = new SolidBrush(Color.FromArgb(55, Color.White));
        using var packet = new SolidBrush(Color.White);
        e.Graphics.FillEllipse(glow, packetX - 5, packetY - 5, 10, 10);
        e.Graphics.FillEllipse(packet, packetX - 2, packetY - 2, 4, 4);
    }
}

internal enum CardActionKind { Settings, PopOut, Pin, Close }

internal sealed class CardActionButton : Control
{
    private bool _hovered;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public CardActionKind Kind { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color IconColor { get; set; } = Color.SlateGray;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color HoverColor { get; set; } = Color.AliceBlue;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ActiveColor { get; set; } = Color.DodgerBlue;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool IsActive { get; set; }

    public CardActionButton()
    {
        Cursor = Cursors.Hand;
        TabStop = true;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw |
                 ControlStyles.SupportsTransparentBackColor, true);
        AccessibleRole = AccessibleRole.PushButton;
    }

    protected override void OnMouseEnter(EventArgs e) { _hovered = true; Invalidate(); base.OnMouseEnter(e); }
    protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        if (_hovered || IsActive)
        {
            using var hoverPath = DrawingTools.Rounded(new RectangleF(1, 1, Width - 2, Height - 2), 8);
            using var hoverBrush = new SolidBrush(IsActive
                ? DrawingTools.Mix(BackColor, ActiveColor, .14f)
                : HoverColor);
            e.Graphics.FillPath(hoverBrush, hoverPath);
        }

        float cx = Width / 2f, cy = Height / 2f;
        using var pen = new Pen(IsActive ? ActiveColor : IconColor, 1.65f)
        { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
        if (Kind == CardActionKind.Close)
        {
            e.Graphics.DrawLine(pen, cx - 5, cy - 5, cx + 5, cy + 5);
            e.Graphics.DrawLine(pen, cx + 5, cy - 5, cx - 5, cy + 5);
        }
        else if (Kind == CardActionKind.PopOut)
        {
            e.Graphics.DrawRectangle(pen, cx - 7, cy - 3, 10, 10);
            e.Graphics.DrawLine(pen, cx - 1, cy + 1, cx + 7, cy - 7);
            e.Graphics.DrawLine(pen, cx + 2, cy - 7, cx + 7, cy - 7);
            e.Graphics.DrawLine(pen, cx + 7, cy - 7, cx + 7, cy - 2);
        }
        else if (Kind == CardActionKind.Pin)
        {
            using var cap = DrawingTools.Rounded(new RectangleF(cx - 6, cy - 8, 12, 7), 2.5f);
            e.Graphics.DrawPath(pen, cap);
            e.Graphics.DrawLine(pen, cx - 5, cy - 1, cx + 5, cy - 1);
            e.Graphics.DrawLine(pen, cx, cy - 1, cx, cy + 7);
            e.Graphics.DrawLine(pen, cx, cy + 7, cx - 2, cy + 4);
        }
        else
        {
            for (int i = -1; i <= 1; i++)
            {
                float y = cy + i * 6;
                float knob = i == -1 ? cx + 4 : i == 0 ? cx - 4 : cx + 2;
                e.Graphics.DrawLine(pen, cx - 8, y, cx + 8, y);
                using var fill = new SolidBrush(BackColor);
                e.Graphics.FillEllipse(fill, knob - 2, y - 2, 4, 4);
                e.Graphics.DrawEllipse(pen, knob - 2, y - 2, 4, 4);
            }
        }
    }
}

internal sealed class CanvasPanel : Panel
{
    public CanvasPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.ResizeRedraw, true);
    }
}

internal sealed class MainForm : Form
{
    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute,
        ref int value, int valueSize);

    private readonly CanvasPanel _toolbar = new() { Dock = DockStyle.Top, Height = 52 };
    private readonly CanvasPanel _board = new() { Dock = DockStyle.Fill, AutoScroll = false };
    private readonly List<MonitorCard> _cards = [];
    private readonly PillButton _addButton = new();
    private readonly PillButton _menuButton = new();
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private AppTheme _theme = Themes.Light;
    private int _layoutColumns = 2;
    private bool _updateCheckRunning;

    public MainForm()
    {
        Text = "PulsePing";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1180, 600);
        MinimumSize = new Size(780, 420);
        Font = Typography.Interface(9.5f);
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

        Controls.Add(_board);
        Controls.Add(_toolbar);
        _toolbar.Paint += (_, e) =>
        {
            using var pen = new Pen(_theme.Border);
            e.Graphics.DrawLine(pen, 0, _toolbar.Height - 1, _toolbar.Width, _toolbar.Height - 1);
        };

        ConfigureButton(_addButton, "+", "Add Host", false);
        _addButton.SetBounds(14, 8, 122, 36);
        _addButton.Click += (_, _) => AddCard(true);
        _toolbar.Controls.Add(_addButton);

        ConfigureButton(_menuButton, "", "•••", false);
        _menuButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _menuButton.SetBounds(ClientSize.Width - 52, 8, 40, 36);
        _menuButton.Click += ShowMenu;
        _toolbar.Controls.Add(_menuButton);

        _board.Resize += (_, _) => LayoutCards();
        Resize += (_, _) =>
        {
            _menuButton.Left = _toolbar.ClientSize.Width - _menuButton.Width - 10;
            LayoutCards();
        };

        ApplyTheme(Themes.Light);
        AddCard(false);
        AddCard(false);
        Shown += async (_, _) =>
        {
            LayoutCards();
            await CheckForUpdatesAsync(showNoUpdateMessage: false);
        };
        FormClosed += (_, _) =>
        {
            _lifetimeCancellation.Cancel();
            _lifetimeCancellation.Dispose();
        };
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyTitleBarTheme();
    }

    private void ApplyTitleBarTheme()
    {
        if (!IsHandleCreated) return;
        bool dark = ReferenceEquals(_theme, Themes.Dark);
        int enabled = dark ? 1 : 0;
        int captionColor = ColorTranslator.ToWin32(dark ? _theme.Toolbar : Color.White);
        int captionTextColor = ColorTranslator.ToWin32(dark ? _theme.Foreground : Color.FromArgb(12, 25, 43));
        try
        {
            if (DwmSetWindowAttribute(Handle, 20, ref enabled, sizeof(int)) != 0)
                DwmSetWindowAttribute(Handle, 19, ref enabled, sizeof(int));
            DwmSetWindowAttribute(Handle, 35, ref captionColor, sizeof(int));
            DwmSetWindowAttribute(Handle, 36, ref captionTextColor, sizeof(int));
        }
        catch { }
    }

    private void ConfigureButton(PillButton button, string glyph, string text, bool primary)
    {
        button.Glyph = glyph;
        button.Text = text;
        button.Font = Typography.Interface(9.5f);
        button.FillColor = primary ? _theme.Accent : DrawingTools.Mix(_theme.Toolbar, _theme.Accent, .06f);
        button.HoverColor = primary ? _theme.AccentHover : DrawingTools.Mix(_theme.Toolbar, _theme.Accent, .13f);
        button.TextColor = primary ? Color.White : _theme.Foreground;
        button.BackColor = _theme.Toolbar;
    }

    private void AddCard(bool focus, HostDefinition? definition = null)
    {
        var card = new MonitorCard(_theme);
        card.RemoveRequested += RemoveCard;
        card.PopOutRequested += PopOutCard;
        if (definition is not null) card.LoadDefinition(definition);
        _cards.Add(card);
        _board.Controls.Add(card);
        ResizeWindowForCards();
        LayoutCards();
        if (focus) card.FocusAddress();
    }

    private void ResizeWindowForCards()
    {
        if (_cards.Count == 0) return;
        const int gap = 12;
        const int minimumCardWidth = 320;
        const int minimumCardHeight = 260;

        Rectangle workArea = Screen.FromControl(this).WorkingArea;
        int chromeWidth = Math.Max(0, Width - ClientSize.Width);
        int chromeHeight = Math.Max(0, Height - ClientSize.Height);
        int maxClientWidth = Math.Max(MinimumSize.Width - chromeWidth, workArea.Width - chromeWidth - 20);
        int maxClientHeight = Math.Max(MinimumSize.Height - chromeHeight, workArea.Height - chromeHeight - 20);
        int maximumRows = Math.Max(1,
            (maxClientHeight - _toolbar.Height - gap) / (minimumCardHeight + gap));

        _layoutColumns = Math.Max(2, (int)Math.Ceiling(_cards.Count / (double)maximumRows));
        int rows = (int)Math.Ceiling(_cards.Count / (double)_layoutColumns);
        int desiredClientWidth = gap + _layoutColumns * (minimumCardWidth + gap);
        int desiredClientHeight = _toolbar.Height + gap + rows * (minimumCardHeight + gap);

        if (WindowState != FormWindowState.Normal) return;
        ClientSize = new Size(
            Math.Min(maxClientWidth, Math.Max(1180, desiredClientWidth)),
            Math.Min(maxClientHeight, Math.Max(600, desiredClientHeight)));

        int right = Left + Width;
        int bottom = Top + Height;
        int adjustedLeft = right > workArea.Right ? Math.Max(workArea.Left, workArea.Right - Width) : Left;
        int adjustedTop = bottom > workArea.Bottom ? Math.Max(workArea.Top, workArea.Bottom - Height) : Top;
        Location = new Point(adjustedLeft, adjustedTop);
    }

    private void RemoveCard(MonitorCard card)
    {
        if (_cards.Count <= 2)
        {
            card.ClearMonitor();
            return;
        }
        _cards.Remove(card);
        _board.Controls.Remove(card);
        card.Dispose();
        ResizeWindowForCards();
        LayoutCards();
    }

    private void PopOutCard(MonitorCard card)
    {
        int originalIndex = _cards.IndexOf(card);
        if (originalIndex < 0) return;
        var popup = new Form
        {
            Text = string.IsNullOrWhiteSpace(card.Address) ? "PulsePing — Monitor" : $"PulsePing — {card.Address}",
            StartPosition = FormStartPosition.CenterScreen,
            ClientSize = new Size(520, 500),
            MinimumSize = new Size(380, 300),
            BackColor = _theme.Background,
            Font = Font,
            Icon = Icon,
            ShowInTaskbar = true
        };
        _cards.Remove(card);
        _board.Controls.Remove(card);
        card.Parent = popup;
        card.Dock = DockStyle.Fill;
        card.Margin = new Padding(12);
        card.SetDetached(true);
        popup.FormClosing += (_, _) =>
        {
            popup.TopMost = false;
            card.SetDetached(false);
            card.Dock = DockStyle.None;
            popup.Controls.Remove(card);
            _board.Controls.Add(card);
            if (!_cards.Contains(card)) _cards.Insert(Math.Min(originalIndex, _cards.Count), card);
            ResizeWindowForCards();
            LayoutCards();
        };
        popup.Controls.Add(card);
        popup.Show();
        popup.Activate();
        ResizeWindowForCards();
        LayoutCards();
    }

    private void LayoutCards()
    {
        if (_board.ClientSize.Width < 20 || _cards.Count == 0) return;
        const int gap = 12;
        int visibleWidth = _board.ClientSize.Width;
        int supportedColumns = Math.Max(1, (visibleWidth - gap) / (300 + gap));
        int columns = Math.Min(Math.Min(_layoutColumns, _cards.Count), supportedColumns);
        int rows = (int)Math.Ceiling(_cards.Count / (double)columns);
        int width = Math.Max(300, (visibleWidth - gap * (columns + 1)) / columns);
        int availableHeight = _board.ClientSize.Height - gap * (rows + 1);
        int height = Math.Max(260, availableHeight / Math.Max(1, rows));
        for (int i = 0; i < _cards.Count; i++)
        {
            int row = i / columns;
            int column = i % columns;
            _cards[i].SetBounds(gap + column * (width + gap), gap + row * (height + gap), width, height);
        }
    }

    private void ShowMenu(object? sender, EventArgs e)
    {
        var menu = new ContextMenuStrip { Font = Typography.Interface(9.5f) };
        menu.Items.Add("Import host list…", null, (_, _) => ImportHosts());
        menu.Items.Add("Export host list…", null, (_, _) => ExportHosts());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Light theme", null, (_, _) => ApplyTheme(Themes.Light));
        menu.Items.Add("Dark theme", null, (_, _) => ApplyTheme(Themes.Dark));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Check for updates…", null, async (_, _) =>
            await CheckForUpdatesAsync(showNoUpdateMessage: true));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("About PulsePing", null, (_, _) =>
        {
            using var about = new AboutDialog(_theme, Icon);
            about.ShowDialog(this);
        });
        menu.Show(_menuButton, new Point(0, _menuButton.Height));
    }

    private async Task CheckForUpdatesAsync(bool showNoUpdateMessage)
    {
        if (_updateCheckRunning)
        {
            if (showNoUpdateMessage)
                MessageBox.Show(this, "An update check is already running.", "PulsePing updates",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _updateCheckRunning = true;
        try
        {
            UpdateCheckResult result = await UpdateChecker.CheckAsync(_lifetimeCancellation.Token);

            if (IsDisposed || Disposing) return;
            if (result.IsUpdateAvailable)
            {
                DialogResult openRelease = MessageBox.Show(this,
                    $"PulsePing {result.LatestVersionText} is available.\n" +
                    $"You are currently using {UpdateChecker.CurrentVersionText}.\n\n" +
                    "Open the verified GitHub release page? PulsePing will not download or install anything automatically.",
                    "PulsePing update available", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (openRelease == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = result.ReleasePage.AbsoluteUri,
                        UseShellExecute = true
                    });
                }
            }
            else if (showNoUpdateMessage)
            {
                MessageBox.Show(this,
                    $"PulsePing {UpdateChecker.CurrentVersionText} is up to date.\n" +
                    $"The latest public release is {result.LatestVersionText}.",
                    "PulsePing updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            if (showNoUpdateMessage && !IsDisposed && !Disposing)
            {
                MessageBox.Show(this,
                    "PulsePing could not check GitHub Releases. Please verify your internet connection and try again.\n\n" +
                    ex.Message,
                    "Update check failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        finally
        {
            _updateCheckRunning = false;
        }
    }

    private void ExportHosts()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Export PulsePing hosts",
            Filter = "PulsePing host list (*.json)|*.json",
            FileName = "pulseping-hosts.json"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var definitions = _cards.Select(card => card.GetDefinition()).
            Where(item => !string.IsNullOrWhiteSpace(item.Address)).ToArray();
        File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(definitions,
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private void ImportHosts()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Import PulsePing hosts",
            Filter = "PulsePing host list (*.json)|*.json"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var definitions = JsonSerializer.Deserialize<HostDefinition[]>(File.ReadAllText(dialog.FileName)) ?? [];
            if (definitions.Length == 0) throw new InvalidDataException("The file contains no hosts.");
            foreach (var card in _cards) card.Dispose();
            _cards.Clear();
            _board.Controls.Clear();
            foreach (var definition in definitions.Take(64)) AddCard(false, definition);
            while (_cards.Count < 2) AddCard(false);
            LayoutCards();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not import the host list.\n\n{ex.Message}",
                "Import failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ApplyTheme(AppTheme theme)
    {
        _theme = theme;
        ApplyTitleBarTheme();
        BackColor = theme.Background;
        _toolbar.BackColor = theme.Toolbar;
        _board.BackColor = theme.Background;
        ConfigureButton(_addButton, "+", "Add Host", false);
        ConfigureButton(_menuButton, "", "•••", false);
        foreach (var card in _cards) card.SetTheme(theme);
        _toolbar.Invalidate();
        _board.Invalidate(true);
    }
}

internal sealed class AboutDialog : Form
{
    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute,
        ref int value, int valueSize);

    private readonly AppTheme _theme;
    private readonly BrandMark _mark = new();
    private readonly System.Windows.Forms.Timer _animationTimer = new() { Interval = 20 };
    private readonly System.Diagnostics.Stopwatch _animationClock = System.Diagnostics.Stopwatch.StartNew();
    private float _animationPhase;

    public AboutDialog(AppTheme theme, Icon? appIcon)
    {
        _theme = theme;
        Text = "About PulsePing";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(740, 700);
        MinimumSize = Size;
        MaximumSize = Size;
        BackColor = theme.Background;
        Font = Typography.Interface(9.5f);
        if (appIcon is not null) Icon = appIcon;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);

        _mark.Accent = theme.Accent;
        _mark.BackColor = theme.Background;
        _mark.SetBounds(32, 26, 72, 72);
        Controls.Add(_mark);

        var close = new PillButton
        {
            Text = "Close",
            Font = Typography.Interface(9.5f, FontStyle.Bold),
            FillColor = theme.Accent,
            HoverColor = theme.AccentHover,
            TextColor = Color.White,
            BackColor = theme.Background,
            DialogResult = DialogResult.OK
        };
        close.SetBounds(590, 642, 118, 40);
        Controls.Add(close);
        AcceptButton = close;
        CancelButton = close;

        _animationTimer.Tick += (_, _) =>
        {
            // Keep the creator-name reflection calm and premium rather than constantly flashing.
            _animationPhase = (float)(_animationClock.Elapsed.TotalSeconds * .20 % 1.0);
            _mark.AnimationPhase = (float)(_animationClock.Elapsed.TotalSeconds * .55 % 1.0);
            _mark.Invalidate();
            Invalidate(new Rectangle(28, 18, 470, 102));
            Invalidate(new Rectangle(30, 540, 680, 96));
        };
        _animationTimer.Start();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _animationTimer.Stop();
        _animationTimer.Dispose();
        base.OnFormClosed(e);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        bool dark = ReferenceEquals(_theme, Themes.Dark);
        int enabled = dark ? 1 : 0;
        int captionColor = ColorTranslator.ToWin32(dark ? _theme.Toolbar : Color.White);
        int captionTextColor = ColorTranslator.ToWin32(dark ? _theme.Foreground : Color.FromArgb(12, 25, 43));
        try
        {
            if (DwmSetWindowAttribute(Handle, 20, ref enabled, sizeof(int)) != 0)
                DwmSetWindowAttribute(Handle, 19, ref enabled, sizeof(int));
            DwmSetWindowAttribute(Handle, 35, ref captionColor, sizeof(int));
            DwmSetWindowAttribute(Handle, 36, ref captionTextColor, sizeof(int));
        }
        catch { }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        using var titleFont = Typography.Royal(24f, FontStyle.Bold);
        using var subtitleFont = Typography.Interface(9.5f);
        using var productFont = Typography.Interface(8f, FontStyle.Bold);
        using var headingFont = Typography.Interface(8.5f, FontStyle.Bold);
        using var bodyFont = Typography.Interface(9.5f);
        using var ownerFont = new Font("Calibri", 13f, FontStyle.Regular, GraphicsUnit.Point);
        using var legalFont = Typography.Interface(8.5f);
        using var foreground = new SolidBrush(_theme.Foreground);
        using var muted = new SolidBrush(_theme.Muted);
        using var accent = new SolidBrush(_theme.Accent);

        float haloPulse = .5f + .5f * MathF.Sin(_animationPhase * MathF.PI * 2f);
        using (var halo = new SolidBrush(Color.FromArgb((int)(8 + haloPulse * 14), _theme.Accent)))
            g.FillEllipse(halo, 22, 16, 92, 92);

        g.DrawString("PulsePing", titleFont, foreground, 122, 22);
        g.DrawString("NETWORK MONITOR FOR WINDOWS", productFont, accent, 124, 69);
        string version = $"Version {UpdateChecker.CurrentVersionText}";
        SizeF versionSize = g.MeasureString(version, subtitleFont);
        g.DrawString(version, subtitleFont, muted, 708 - versionSize.Width, 69);
        using (var separator = new Pen(_theme.Separator))
            g.DrawLine(separator, 32, 118, 708, 118);

        DrawHeading(g, "ABOUT THE PRODUCT", headingFont, accent, 32, 142);
        DrawBody(g,
            "PulsePing is a focused, high-performance Windows workspace for monitoring the reachability, " +
            "latency and stability of individual network hosts in real time.",
            bodyFont, _theme.Foreground, new Rectangle(32, 169, 676, 62));

        DrawHeading(g, "CORE CAPABILITIES", headingFont, accent, 32, 248);
        DrawBody(g,
            "• Multi-host ICMP monitoring with configurable intervals and timeouts\n" +
            "• Animated response stream, latency history and packet-loss statistics\n" +
            "• Per-host pause, resume, independent pop-out and always-on-top pinning\n" +
            "• Import and export, responsive cards, plus light and dark themes\n" +
            "• Background update checks on every launch through GitHub Releases",
            bodyFont, _theme.Foreground, new Rectangle(32, 275, 676, 120));

        DrawHeading(g, "PRIVACY & RESPONSIBLE DESIGN", headingFont, accent, 32, 414);
        DrawBody(g,
            "PulsePing pings only user-entered addresses and performs no range scanning, discovery or telemetry. " +
            "Startup update checks contact GitHub without sending monitored targets or ping history.",
            bodyFont, _theme.Foreground, new Rectangle(32, 441, 676, 72));

        using (var ownerPath = DrawingTools.Rounded(new RectangleF(32, 536, 676, 88), 14))
        using (var ownerFill = new SolidBrush(DrawingTools.Mix(_theme.Card, _theme.Accent, .07f)))
            g.FillPath(ownerFill, ownerPath);

        const string ownerPrefix = "Created and developed by ";
        const string ownerName = "Shubham Kasar";
        const float ownerX = 54;
        const float ownerY = 548;
        g.DrawString(ownerPrefix, ownerFont, foreground, ownerX, ownerY);
        float nameX = ownerX + g.MeasureString(ownerPrefix, ownerFont).Width - 3;
        DrawShiningName(g, ownerName, ownerFont, nameX, ownerY,
            _theme.Foreground, DrawingTools.Mix(Color.White, _theme.Accent, .22f), _animationPhase);

        g.DrawString("Company / Publisher: Shubham Kasar", legalFont, muted, 54, 580);
        g.DrawString("© 2026 Shubham Kasar. All rights reserved.", legalFont, muted, 54, 601);
    }

    private static void DrawHeading(Graphics g, string text, Font font, Brush brush, float x, float y) =>
        g.DrawString(text, font, brush, x, y);

    private static void DrawBody(Graphics g, string text, Font font, Color color, Rectangle bounds) =>
        TextRenderer.DrawText(g, text, font, bounds, color,
            TextFormatFlags.WordBreak | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix |
            TextFormatFlags.TextBoxControl);

    private static void DrawShiningName(Graphics g, string text, Font font, float x, float y,
        Color baseColor, Color reflectionColor, float phase)
    {
        using var letterPath = new GraphicsPath();
        float emSize = font.SizeInPoints * g.DpiY / 72f;
        letterPath.AddString(text, font.FontFamily, (int)font.Style, emSize,
            new PointF(x, y), StringFormat.GenericTypographic);

        RectangleF bounds = letterPath.GetBounds();

        using (var nameBrush = new SolidBrush(baseColor))
            g.FillPath(nameBrush, letterPath);

        // The moving reflection is clipped to the glyphs, so no rectangle ever appears
        // behind the name. Layered diagonal bands make the highlight feel softly reflected.
        float reflectionX = bounds.Left - 42f + (bounds.Width + 84f) * phase;
        GraphicsState state = g.Save();
        g.SetClip(letterPath, CombineMode.Intersect);
        DrawReflectionBand(g, bounds, reflectionX, 15f, Color.FromArgb(32, reflectionColor));
        DrawReflectionBand(g, bounds, reflectionX, 7f, Color.FromArgb(72, reflectionColor));
        DrawReflectionBand(g, bounds, reflectionX, 2.2f, Color.FromArgb(150, reflectionColor));
        g.Restore(state);
    }

    private static void DrawReflectionBand(Graphics g, RectangleF bounds, float x, float halfWidth, Color color)
    {
        const float slant = 13f;
        PointF[] band =
        {
            new(x - halfWidth, bounds.Top - 4f),
            new(x + halfWidth, bounds.Top - 4f),
            new(x + halfWidth + slant, bounds.Bottom + 4f),
            new(x - halfWidth + slant, bounds.Bottom + 4f)
        };
        using var brush = new SolidBrush(color);
        g.FillPolygon(brush, band);
    }
}

internal sealed record HostDefinition(string Address, int IntervalSeconds, int TimeoutMs, bool Paused);
internal sealed record PingEvent(DateTime Time, bool Success, long Latency);

internal sealed class HostSettingsDialog : Form
{
    private readonly NumericUpDown _interval = new() { Minimum = 1, Maximum = 60, Width = 150 };
    private readonly NumericUpDown _timeout = new() { Minimum = 100, Maximum = 5000, Increment = 100, Width = 150 };
    private readonly CheckBox _paused = new() { Text = "Pause this monitor", AutoSize = true };

    public int IntervalSeconds => (int)_interval.Value;
    public int TimeoutMs => (int)_timeout.Value;
    public bool Paused => _paused.Checked;

    public HostSettingsDialog(AppTheme theme, int interval, int timeout, bool paused)
    {
        Text = "Monitor settings";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(420, 290);
        BackColor = theme.Background;
        ForeColor = theme.Foreground;
        Font = Typography.Interface(9.5f);

        var title = new Label
        {
            Text = "Monitor settings",
            Font = Typography.Royal(18f),
            ForeColor = theme.Foreground,
            BackColor = theme.Background,
            AutoSize = true,
            Location = new Point(28, 24)
        };
        var caption = new Label
        {
            Text = "Tune one host without affecting the other monitors.",
            ForeColor = theme.Muted,
            BackColor = theme.Background,
            AutoSize = true,
            Location = new Point(30, 59)
        };
        Controls.Add(title);
        Controls.Add(caption);

        AddField("Ping interval", "seconds", _interval, 98, theme);
        AddField("Timeout", "milliseconds", _timeout, 145, theme);
        _interval.Value = Math.Clamp(interval, 1, 60);
        _timeout.Value = Math.Clamp(timeout, 100, 5000);
        _interval.Font = _timeout.Font = Typography.Interface(9.5f);
        _interval.BackColor = _timeout.BackColor = theme.Input;
        _interval.ForeColor = _timeout.ForeColor = theme.Foreground;

        _paused.Checked = paused;
        _paused.Location = new Point(31, 198);
        _paused.BackColor = theme.Background;
        _paused.ForeColor = theme.Foreground;
        Controls.Add(_paused);

        var cancel = new PillButton
        {
            Text = "Cancel", Font = Typography.Interface(9.5f),
            FillColor = DrawingTools.Mix(theme.Card, theme.Accent, .06f),
            HoverColor = DrawingTools.Mix(theme.Card, theme.Accent, .13f),
            TextColor = theme.Foreground, BackColor = theme.Background,
            DialogResult = DialogResult.Cancel
        };
        cancel.SetBounds(204, 238, 88, 36);
        Controls.Add(cancel);

        var save = new PillButton
        {
            Text = "Save", Glyph = "✓", Font = Typography.Interface(9.5f),
            FillColor = theme.Accent, HoverColor = theme.AccentHover,
            TextColor = Color.White, BackColor = theme.Background,
            DialogResult = DialogResult.OK
        };
        save.SetBounds(302, 238, 88, 36);
        Controls.Add(save);
        AcceptButton = save;
        CancelButton = cancel;
    }

    private void AddField(string label, string suffix, NumericUpDown field, int y, AppTheme theme)
    {
        var name = new Label
        {
            Text = label, AutoSize = true, Location = new Point(30, y + 7),
            ForeColor = theme.Foreground, BackColor = theme.Background,
            Font = Typography.Interface(9.5f)
        };
        field.Location = new Point(177, y + 2);
        var unit = new Label
        {
            Text = suffix, AutoSize = true, Location = new Point(333, y + 7),
            ForeColor = theme.Muted, BackColor = theme.Background
        };
        Controls.Add(name);
        Controls.Add(field);
        Controls.Add(unit);
    }
}

internal sealed class MonitorCard : Control
{
    private readonly TextBox _address = new();
    private readonly PillButton _pingButton = new();
    private readonly PillButton _pauseButton = new() { Visible = false };
    private readonly CardActionButton _removeButton = new() { Kind = CardActionKind.Close, AccessibleName = "Remove monitor" };
    private readonly CardActionButton _popButton = new() { Kind = CardActionKind.PopOut, AccessibleName = "Open monitor in a separate window" };
    private readonly CardActionButton _settingsButton = new() { Kind = CardActionKind.Settings, AccessibleName = "Monitor settings" };
    private readonly ToolTip _toolTips = new()
    {
        InitialDelay = 320,
        ReshowDelay = 100,
        AutoPopDelay = 5000,
        ShowAlways = true
    };
    private readonly System.Windows.Forms.Timer _pingTimer = new() { Interval = 1000 };
    private readonly System.Windows.Forms.Timer _animationTimer = new() { Interval = 15 };
    private readonly System.Diagnostics.Stopwatch _animationClock = System.Diagnostics.Stopwatch.StartNew();
    private readonly List<PingEvent> _events = [];
    private readonly List<long?> _history = [];
    private AppTheme _theme;
    private string _target = "";
    private string _status = "";
    private bool _probing;
    private bool _paused;
    private bool _detached;
    private int _intervalSeconds = 1;
    private int _timeoutMs = 900;
    private int _attempts;
    private int _failures;
    private float _phase;
    private float _pauseButtonWidth = 86;
    private float _pauseButtonTargetWidth = 86;
    private double _lastEventAnimationSeconds = double.NegativeInfinity;

    public event Action<MonitorCard>? RemoveRequested;
    public event Action<MonitorCard>? PopOutRequested;
    public string Address => _target;

    public MonitorCard(AppTheme theme)
    {
        _theme = theme;
        Font = Typography.Interface(9.5f);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

        _address.BorderStyle = BorderStyle.None;
        _address.Font = Typography.Interface(11f);
        _address.PlaceholderText = "Enter a hostname or IP address";
        _address.TextChanged += (_, _) =>
        {
            UpdateAddressPresentation();
            PositionChildren();
            Invalidate(new Rectangle(0, Math.Max(0, Height - 58), Width, 58));
        };
        _address.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; StartOrPing(); } };
        Controls.Add(_address);

        SetupMiniButton(_pingButton, "▶", "Ping");
        _pingButton.Click += (_, _) => StartOrPing();
        Controls.Add(_pingButton);

        SetupMiniButton(_pauseButton, "Ⅱ", "Pause");
        _pauseButton.Click += (_, _) => SetPaused(!_paused);
        Controls.Add(_pauseButton);

        _removeButton.Click += (_, _) => RemoveRequested?.Invoke(this);
        Controls.Add(_removeButton);
        BindFeatureTip(_removeButton, () => "Remove monitor");

        _popButton.Click += (_, _) =>
        {
            if (!_detached)
            {
                PopOutRequested?.Invoke(this);
                return;
            }

            if (FindForm() is not Form popup) return;
            popup.TopMost = !popup.TopMost;
            _popButton.IsActive = popup.TopMost;
            _popButton.AccessibleName = popup.TopMost
                ? "Unpin monitor from the top"
                : "Keep monitor above other windows";
            _popButton.Invalidate();
        };
        Controls.Add(_popButton);
        BindFeatureTip(_popButton, () => _detached
            ? _popButton.IsActive ? "Unpin from always on top" : "Pin above other applications"
            : "Open in a separate window");

        _settingsButton.Click += (_, _) => ShowSettings();
        Controls.Add(_settingsButton);
        BindFeatureTip(_settingsButton, () => "Monitor settings");

        _pingTimer.Tick += async (_, _) => await ProbeAsync();
        _animationTimer.Tick += (_, _) =>
        {
            double now = _animationClock.Elapsed.TotalSeconds;
            _phase = (float)(now * .38 % 1.0);
            if (Math.Abs(_pauseButtonWidth - _pauseButtonTargetWidth) > .2f)
            {
                _pauseButtonWidth += (_pauseButtonTargetWidth - _pauseButtonWidth) * .2f;
                if (Math.Abs(_pauseButtonWidth - _pauseButtonTargetWidth) < .35f)
                    _pauseButtonWidth = _pauseButtonTargetWidth;
                PositionChildren();
                Invalidate(new Rectangle(0, Math.Max(0, Height - 62), Width, 62));
            }
            if (string.IsNullOrWhiteSpace(_target) || now - _lastEventAnimationSeconds < .82)
                Invalidate();
        };
        _animationTimer.Start();
        SetTheme(theme);
    }

    private void SetupMiniButton(PillButton button, string glyph, string text)
    {
        button.Glyph = glyph;
        button.Text = text;
        button.Font = Typography.Interface(9.5f);
    }

    private void BindFeatureTip(Control control, Func<string> text)
    {
        control.MouseEnter += (_, _) =>
        {
            _toolTips.Hide(control);
            _toolTips.Show(text(), control,
                new Point(control.Width / 2, control.Height + 5), 5000);
        };
        control.MouseLeave += (_, _) => _toolTips.Hide(control);
        control.MouseDown += (_, _) => _toolTips.Hide(control);
    }

    public void FocusAddress() { _address.Focus(); }

    public void SetDetached(bool detached)
    {
        _detached = detached;
        _popButton.Kind = detached ? CardActionKind.Pin : CardActionKind.PopOut;
        _popButton.IsActive = detached && FindForm()?.TopMost == true;
        _popButton.AccessibleName = detached
            ? "Keep monitor above other windows"
            : "Open monitor in a separate window";
        _popButton.Invalidate();
    }

    public HostDefinition GetDefinition() => new(_target, _intervalSeconds, _timeoutMs, _paused);

    public void LoadDefinition(HostDefinition definition)
    {
        _target = definition.Address?.Trim() ?? "";
        _intervalSeconds = Math.Clamp(definition.IntervalSeconds, 1, 60);
        _timeoutMs = Math.Clamp(definition.TimeoutMs, 100, 5000);
        _address.Text = _target;
        SetPaused(definition.Paused);
        if (!_paused && !string.IsNullOrWhiteSpace(_target))
        {
            _pingTimer.Interval = _intervalSeconds * 1000;
            _pingTimer.Start();
            _ = ProbeAsync();
        }
    }

    public void SetPaused(bool paused)
    {
        _paused = paused;
        if (_paused)
        {
            _pingTimer.Stop();
            if (!string.IsNullOrWhiteSpace(_target)) _status = "Paused";
        }
        else if (!string.IsNullOrWhiteSpace(_target))
        {
            _pingTimer.Interval = _intervalSeconds * 1000;
            _pingTimer.Start();
            _status = "Waiting";
            _ = ProbeAsync();
        }
        UpdateChildSurfaces();
        Invalidate();
    }

    private void ShowSettings()
    {
        using var dialog = new HostSettingsDialog(_theme, _intervalSeconds, _timeoutMs, _paused);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
        _intervalSeconds = dialog.IntervalSeconds;
        _timeoutMs = dialog.TimeoutMs;
        _pingTimer.Interval = _intervalSeconds * 1000;
        SetPaused(dialog.Paused);
    }

    public void ClearMonitor()
    {
        _pingTimer.Stop();
        _target = "";
        _status = "";
        _paused = false;
        _attempts = _failures = 0;
        _events.Clear();
        _history.Clear();
        _address.Clear();
        _address.Focus();
        UpdateChildSurfaces();
        Invalidate();
    }

    public void SetTheme(AppTheme theme)
    {
        _theme = theme;
        BackColor = theme.Background;
        _address.BackColor = theme.Input;
        _address.ForeColor = theme.Foreground;
        _pingButton.FillColor = theme.Accent;
        _pingButton.HoverColor = theme.AccentHover;
        _pingButton.TextColor = Color.White;
        _pauseButton.TextColor = theme.Accent;
        foreach (var button in new[] { _removeButton, _popButton, _settingsButton })
        {
            button.HoverColor = DrawingTools.Mix(theme.Card, theme.Accent, .10f);
            button.IconColor = theme.Muted;
            button.ActiveColor = theme.Accent;
        }
        UpdateChildSurfaces();
        Invalidate();
    }

    private void UpdateChildSurfaces()
    {
        Color cardColor = _status == "Online" ? _theme.OnlineCard
            : _status == "Offline" ? _theme.OfflineCard
            : _theme.Card;
        Color inputColor = _status == "Online"
            ? DrawingTools.Mix(_theme.Input, _theme.Online, .06f)
            : _status == "Offline"
                ? DrawingTools.Mix(_theme.Input, _theme.Offline, .06f)
                : _theme.Input;
        _address.BackColor = inputColor;
        UpdateAddressPresentation();
        _pingButton.BackColor = cardColor;
        _pauseButton.Visible = !string.IsNullOrWhiteSpace(_target);
        _pauseButton.Glyph = _paused ? "▶" : "Ⅱ";
        _pauseButton.Text = _paused ? "Resume" : "Pause";
        _pauseButtonTargetWidth = _pauseButton.Visible ? MeasurePauseButtonWidth() : 86;
        if (!_pauseButton.Visible) _pauseButtonWidth = _pauseButtonTargetWidth;
        _pauseButton.FillColor = DrawingTools.Mix(cardColor, _theme.Accent, .08f);
        _pauseButton.HoverColor = DrawingTools.Mix(cardColor, _theme.Accent, .16f);
        _pauseButton.TextColor = _theme.Accent;
        _pauseButton.BackColor = cardColor;
        _pauseButton.AccessibleName = _paused ? "Resume monitor" : "Pause monitor";
        _pauseButton.Invalidate();
        foreach (var button in new[] { _removeButton, _popButton, _settingsButton })
        {
            button.BackColor = cardColor;
        }
        PositionChildren();
    }

    private int MeasurePauseButtonWidth()
    {
        string label = $"{_pauseButton.Glyph}  {_pauseButton.Text}";
        Size measured = TextRenderer.MeasureText(label, _pauseButton.Font,
            new Size(int.MaxValue, 34), TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        return Math.Clamp(measured.Width + 24, 86, 124);
    }

    private bool AddressIsAccepted =>
        !string.IsNullOrWhiteSpace(_target) &&
        string.Equals(_address.Text.Trim(), _target, StringComparison.OrdinalIgnoreCase);

    private void UpdateAddressPresentation()
    {
        bool accepted = AddressIsAccepted;
        _address.TextAlign = accepted ? HorizontalAlignment.Center : HorizontalAlignment.Left;
        _address.ForeColor = accepted
            ? _status == "Online" ? _theme.Online
                : _status == "Offline" ? _theme.Offline
                : _theme.Foreground
            : _theme.Foreground;
        _address.AccessibleDescription = accepted
            ? "Accepted monitoring target. Select or type to edit."
            : "Enter a hostname or IP address, then press Ping.";
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        PositionChildren();
    }

    private void PositionChildren()
    {
        int bottom = Height - 48;
        int pauseWidth = Math.Max(1, (int)Math.Round(_pauseButtonWidth));
        _pingButton.SetBounds(Math.Max(110, Width - 102), bottom, 86, 34);
        _pauseButton.SetBounds(Math.Max(18, Width - 108 - pauseWidth), bottom, pauseWidth, 34);
        int addressReserve = _pauseButton.Visible ? 146 + pauseWidth : 140;
        int addressWidth = Math.Max(70, Width - addressReserve);
        if (AddressIsAccepted && addressWidth >= 150)
            _address.SetBounds(48, bottom + 3, addressWidth - 52, 25);
        else
            _address.SetBounds(22, bottom + 3, addressWidth, 25);
        _removeButton.SetBounds(Width - 49, 11, 32, 32);
        _popButton.SetBounds(Width - 85, 11, 32, 32);
        _settingsButton.SetBounds(Width - 121, 11, 32, 32);
    }

    private void StartOrPing()
    {
        string address = _address.Text.Trim();
        if (string.IsNullOrWhiteSpace(address)) { _address.Focus(); return; }
        if (!string.Equals(address, _target, StringComparison.OrdinalIgnoreCase))
        {
            _target = address;
            _status = "Waiting";
            _attempts = _failures = 0;
            _events.Clear();
            _history.Clear();
        }
        if (!string.Equals(_address.Text, address, StringComparison.Ordinal))
            _address.Text = address;
        _paused = false;
        UpdateChildSurfaces();
        _pingTimer.Interval = _intervalSeconds * 1000;
        _pingTimer.Start();
        _ = ProbeAsync();
    }

    private async Task ProbeAsync()
    {
        if (_probing || _paused || string.IsNullOrWhiteSpace(_target)) return;
        _probing = true;
        _status = "Waiting";
        Invalidate();
        bool success = false;
        long latency = 0;
        try
        {
            using var ping = new Ping();
            PingReply reply = await ping.SendPingAsync(_target, _timeoutMs);
            success = reply.Status == IPStatus.Success;
            latency = success ? Math.Max(1, reply.RoundtripTime) : 0;
        }
        catch { success = false; }

        if (IsDisposed) return;
        _attempts++;
        if (!success) _failures++;
        _status = success ? "Online" : "Offline";
        _events.Add(new PingEvent(DateTime.Now, success, latency));
        _lastEventAnimationSeconds = _animationClock.Elapsed.TotalSeconds;
        _history.Add(success ? latency : null);
        if (_events.Count > 50) _events.RemoveAt(0);
        if (_history.Count > 60) _history.RemoveAt(0);
        _probing = false;
        UpdateChildSurfaces();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        bool online = _status == "Online";
        bool offline = _status == "Offline";
        Color cardColor = online ? _theme.OnlineCard : offline ? _theme.OfflineCard : _theme.Card;
        Color borderColor = online ? _theme.Online : offline ? _theme.Offline : _theme.Border;
        using (var path = DrawingTools.Rounded(new RectangleF(2, 2, Width - 5, Height - 5), 18))
        using (var fill = new SolidBrush(cardColor))
        using (var border = new Pen(borderColor, 1.2f))
        {
            g.FillPath(fill, path);
            g.DrawPath(border, path);
        }
        using (var separator = new Pen(_theme.Separator))
            g.DrawLine(separator, 18, Height - 58, Width - 18, Height - 58);
        int inputReserve = _pauseButton.Visible
            ? 139 + Math.Max(1, (int)Math.Round(_pauseButtonWidth))
            : 133;
        Color inputColor = online
            ? DrawingTools.Mix(_theme.Input, _theme.Online, .06f)
            : offline
                ? DrawingTools.Mix(_theme.Input, _theme.Offline, .06f)
                : _theme.Input;
        using (var inputPath = DrawingTools.Rounded(
            new RectangleF(17, Height - 52, Math.Max(60, Width - inputReserve), 40), 6))
        using (var inputFill = new SolidBrush(inputColor))
        {
            g.FillPath(inputFill, inputPath);
            if (online || offline || AddressIsAccepted)
            {
                Color acceptedColor = online ? _theme.Online : offline ? _theme.Offline : _theme.Accent;
                using var inputBorder = new Pen(acceptedColor, 1.15f);
                g.DrawPath(inputBorder, inputPath);
            }
        }

        if (AddressIsAccepted)
        {
            Color acceptedColor = online ? _theme.Online : offline ? _theme.Offline : _theme.Accent;
            float iconX = 34;
            float iconY = Height - 32;
            using var badgeFill = new SolidBrush(Color.FromArgb(22, acceptedColor));
            using var badgeBorder = new Pen(Color.FromArgb(150, acceptedColor), 1f);
            using var check = new Pen(acceptedColor, 1.65f)
            { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            g.FillEllipse(badgeFill, iconX - 8, iconY - 8, 16, 16);
            g.DrawEllipse(badgeBorder, iconX - 8, iconY - 8, 16, 16);
            g.DrawLines(check,
            [
                new PointF(iconX - 4, iconY),
                new PointF(iconX - 1, iconY + 3),
                new PointF(iconX + 5, iconY - 4)
            ]);
        }

        if (string.IsNullOrWhiteSpace(_target)) DrawTopology(g);
        else DrawMonitor(g, cardColor);
    }

    private void DrawMonitor(Graphics g, Color cardColor)
    {
        using var titleFont = Typography.Royal(16f, FontStyle.Bold);
        using var statusFont = Typography.Interface(9.5f, FontStyle.Bold);
        using var smallFont = Typography.Interface(7.5f);
        using var valueFont = Typography.Interface(9.5f, FontStyle.Bold);
        using var foreground = new SolidBrush(_theme.Foreground);
        using var muted = new SolidBrush(_theme.Muted);
        Color stateColor = _status == "Online" ? _theme.Online : _status == "Offline" ? _theme.Offline : _theme.Muted;
        using var state = new SolidBrush(stateColor);

        g.DrawString(_target, titleFont, foreground, 22, 19);
        string statusText = _status.ToUpperInvariant();
        SizeF statusSize = g.MeasureString(statusText, statusFont);
        float statusY = 48;
        float statusX = Width - 20 - statusSize.Width;
        g.FillEllipse(state, statusX - 13, statusY + 5, 8, 8);
        g.DrawString(statusText, statusFont, state, statusX, statusY);
        float firstY = 82;
        float statsLabelY = Height - 130;
        float statsValueY = Height - 112;
        DrawResponseStream(g, cardColor, firstY, statsLabelY - 6);

        var good = _history.Where(x => x.HasValue).Select(x => x!.Value).ToArray();
        string average = good.Length == 0 ? "—" : $"{good.Average():0} ms";
        string loss = _attempts == 0 ? "0%" : $"{_failures * 100.0 / _attempts:0}%";
        string[] labels = ["AVERAGE", "PACKET LOSS", "INTERVAL"];
        string[] values = [average, loss, $"{_intervalSeconds}s"];
        for (int i = 0; i < 3; i++)
        {
            float center = Width * (i + 1) / 4f;
            DrawCentered(g, labels[i], smallFont, muted, center, statsLabelY);
            DrawCentered(g, values[i], valueFont, foreground, center, statsValueY);
        }
        DrawChart(g);
    }

    private void DrawResponseStream(Graphics g, Color cardColor, float firstY, float bottomY)
    {
        if (_events.Count == 0) return;
        float rowHeight = Height < 340 ? 26 : 30;
        int maxRows = Math.Max(1, (int)Math.Floor((bottomY - firstY) / rowHeight));
        var visible = _events.TakeLast(maxRows).Reverse().ToArray();
        long latencyScale = Math.Max(10,
            visible.Where(item => item.Success).Select(item => item.Latency).DefaultIfEmpty(10).Max());

        double age = _animationClock.Elapsed.TotalSeconds - _lastEventAnimationSeconds;
        float animation = Math.Clamp((float)(age / .72), 0, 1);
        float eased = animation * animation * animation *
            (animation * (animation * 6 - 15) + 10);
        float pulse = MathF.Sin(animation * MathF.PI);
        using var timeFont = Typography.Interface(8.5f);
        using var valueFont = Typography.Interface(8.5f, FontStyle.Bold);

        for (int index = 0; index < visible.Length; index++)
        {
            PingEvent item = visible[index];
            bool newest = index == 0;
            Color eventColor = item.Success ? _theme.Online : _theme.Offline;
            float rowY = firstY + index * rowHeight;
            if (!newest && animation < 1) rowY -= rowHeight * (1 - eased);
            if (newest && animation < 1) rowY -= 7 * (1 - eased);
            float slide = newest ? 8 * (1 - eased) : 0;
            float rowX = 22 + slide;
            float rowWidth = Math.Max(120, Width - 44 - slide);
            int opacity = newest ? (int)(38 + 217 * eased) : 220;

            using (var rowPath = DrawingTools.Rounded(
                new RectangleF(rowX, rowY + 1, rowWidth, rowHeight - 3), 8))
            using (var rowFill = new SolidBrush(Color.FromArgb(newest ? (int)(8 + 18 * eased) : 8, eventColor)))
            {
                g.FillPath(rowFill, rowPath);

                if (newest && animation < 1)
                {
                    float sheenWidth = Math.Min(110, rowWidth * .28f);
                    float sheenX = rowX - sheenWidth + (rowWidth + sheenWidth * 2) * eased;
                    var sheenRect = new RectangleF(sheenX, rowY + 1, sheenWidth, rowHeight - 3);
                    using var sheen = new LinearGradientBrush(sheenRect,
                        Color.FromArgb(0, eventColor), Color.FromArgb((int)(24 * pulse), eventColor),
                        LinearGradientMode.Horizontal);
                    var saved = g.Save();
                    g.SetClip(rowPath);
                    g.FillRectangle(sheen, sheenRect);
                    g.Restore(saved);
                }
            }

            float middleY = rowY + rowHeight / 2f;
            using var dot = new SolidBrush(Color.FromArgb(opacity, eventColor));
            g.FillEllipse(dot, rowX + 8, middleY - 3, 6, 6);
            if (newest && animation < 1)
            {
                float rippleRadius = 4 + eased * 8;
                using var ripple = new Pen(Color.FromArgb((int)(58 * pulse), eventColor), 1.05f);
                g.DrawEllipse(ripple, rowX + 11 - rippleRadius, middleY - rippleRadius,
                    rippleRadius * 2, rippleRadius * 2);
            }

            using var timeBrush = new SolidBrush(Color.FromArgb(opacity, _theme.Muted));
            using var valueBrush = new SolidBrush(Color.FromArgb(opacity, eventColor));
            var timeRect = new RectangleF(rowX + 22, rowY + 4, 78, rowHeight - 7);
            g.DrawString(item.Time.ToString("HH:mm:ss"), timeFont, timeBrush, timeRect);

            float trackLeft = rowX + 101;
            float trackRight = rowX + rowWidth - 67;
            float trackWidth = Math.Max(24, trackRight - trackLeft);
            using var track = new Pen(Color.FromArgb(38, _theme.Muted), 1.2f)
            { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(track, trackLeft, middleY, trackLeft + trackWidth, middleY);
            float fraction = item.Success
                ? Math.Clamp(item.Latency / (float)latencyScale, .08f, 1f)
                : 1f;
            if (newest) fraction *= eased;
            float responseRight = trackLeft + trackWidth * fraction;
            using var responseGradient = new LinearGradientBrush(
                new RectangleF(trackLeft, middleY - 2, Math.Max(1, responseRight - trackLeft), 4),
                Color.FromArgb(Math.Max(20, opacity / 2), eventColor),
                Color.FromArgb(opacity, eventColor), LinearGradientMode.Horizontal);
            using var response = new Pen(responseGradient, 2.4f)
            { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(response, trackLeft, middleY, responseRight, middleY);

            if (newest && animation < 1 && fraction > 0)
            {
                float glowRadius = 3.2f + pulse * 1.8f;
                using var barGlow = new SolidBrush(Color.FromArgb((int)(34 * pulse), eventColor));
                using var barCore = new SolidBrush(Color.FromArgb(opacity, eventColor));
                g.FillEllipse(barGlow, responseRight - glowRadius, middleY - glowRadius,
                    glowRadius * 2, glowRadius * 2);
                g.FillEllipse(barCore, responseRight - 1.7f, middleY - 1.7f, 3.4f, 3.4f);
            }

            string value = item.Success ? $"{item.Latency} ms" : "TIMEOUT";
            using var alignment = new StringFormat
            { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
            var valueRect = new RectangleF(rowX + rowWidth - 64, rowY, 58, rowHeight - 2);
            g.DrawString(value, valueFont, valueBrush, valueRect, alignment);
        }
    }

    private static void DrawCentered(Graphics g, string text, Font font, Brush brush, float centerX, float y)
    {
        var size = g.MeasureString(text, font);
        g.DrawString(text, font, brush, centerX - size.Width / 2, y);
    }

    private void DrawChart(Graphics g)
    {
        if (_history.Count < 2) return;
        var data = _history.TakeLast(30).ToArray();
        long max = Math.Max(1, data.Where(x => x.HasValue).Select(x => x!.Value).DefaultIfEmpty(1).Max());
        float left = 24, right = Width - 24, top = Height - 87, bottom = Height - 68;
        var points = data.Select((value, i) => new PointF(
            left + i * (right - left) / Math.Max(1, data.Length - 1),
            value.HasValue ? bottom - value.Value / (float)max * (bottom - top) : bottom)).ToArray();
        using var fillPath = new GraphicsPath();
        fillPath.AddLines(points);
        fillPath.AddLine(points[^1], new PointF(right, bottom));
        fillPath.AddLine(new PointF(right, bottom), new PointF(left, bottom));
        fillPath.CloseFigure();
        using var area = new SolidBrush(DrawingTools.Mix(_theme.Card, _theme.Accent, .12f));
        using var line = new Pen(_theme.Accent, 1.8f) { LineJoin = LineJoin.Round };
        g.FillPath(area, fillPath);
        g.DrawLines(line, points);
    }

    private void DrawTopology(Graphics g)
    {
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        float centerX = Width / 2f;
        float centerY = Math.Max(82, (Height - 62) / 2f);
        float spanX = Math.Min(175, Math.Max(105, Width * .29f));
        float spanY = Math.Min(90, Math.Max(52, (Height - 120) * .22f));
        PointF[] points =
        [
            new(centerX, centerY), new(centerX - spanX, centerY - spanY * .52f),
            new(centerX - spanX * .82f, centerY + spanY),
            new(centerX + spanX * .78f, centerY - spanY),
            new(centerX + spanX, centerY + spanY * .56f)
        ];

        for (int i = 1; i < points.Length; i++)
        {
            using var glow = new Pen(Color.FromArgb(24, _theme.Network), 4.2f)
            { StartCap = LineCap.Round, EndCap = LineCap.Round };
            using var link = new Pen(DrawingTools.Mix(_theme.Card, _theme.Network, .36f), 1.05f)
            { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(glow, points[0], points[i]);
            g.DrawLine(link, points[0], points[i]);
        }

        for (int i = 1; i < points.Length; i++)
        {
            using var halo = new SolidBrush(Color.FromArgb(18, _theme.Network));
            using var node = new SolidBrush(DrawingTools.Mix(_theme.Card, _theme.Network, .07f));
            using var outline = new Pen(DrawingTools.Mix(_theme.Card, _theme.Network, .66f), 1.1f);
            g.FillEllipse(halo, points[i].X - 25, points[i].Y - 25, 50, 50);
            g.FillEllipse(node, points[i].X - 20, points[i].Y - 20, 40, 40);
            g.DrawEllipse(outline, points[i].X - 20, points[i].Y - 20, 40, 40);
            DrawDevice(g, i, points[i], outline);
        }

        float pulse = .5f + .5f * MathF.Sin(_phase * MathF.PI * 2);
        float pulseRadius = 33 + pulse * 4;
        using (var pulsePen = new Pen(Color.FromArgb((int)(18 + pulse * 25), _theme.Network), 1.2f))
            g.DrawEllipse(pulsePen, centerX - pulseRadius, centerY - pulseRadius,
                pulseRadius * 2, pulseRadius * 2);
        using (var switchPath = DrawingTools.Rounded(new RectangleF(centerX - 29, centerY - 19, 58, 38), 9))
        using (var switchFill = new SolidBrush(DrawingTools.Mix(_theme.Card, _theme.Network, .09f)))
        using (var switchPen = new Pen(DrawingTools.Mix(_theme.Card, _theme.Network, .69f), 1.1f))
        {
            g.FillPath(switchFill, switchPath);
            g.DrawPath(switchPen, switchPath);
        }
        using var dotBrush = new SolidBrush(DrawingTools.Mix(_theme.Card, _theme.Network, .78f));
        for (int i = -15; i <= 15; i += 10) g.FillEllipse(dotBrush, centerX + i - 2, centerY + 4, 4, 4);

        for (int i = 1; i < points.Length; i++)
        {
            float cycle = (_phase + i * .21f) % 1f;
            bool inbound = i % 2 == 0;
            float progress = inbound ? 1 - cycle : cycle;
            float visibility = MathF.Sin(cycle * MathF.PI);
            PointF packet = LinearPoint(points[0], points[i], progress);
            for (int trailIndex = 4; trailIndex >= 1; trailIndex--)
            {
                float offset = trailIndex * .014f;
                float trailProgress = inbound
                    ? Math.Min(1, progress + offset)
                    : Math.Max(0, progress - offset);
                PointF trailPoint = LinearPoint(points[0], points[i], trailProgress);
                int alpha = (int)(visibility * (38 - trailIndex * 6));
                float radius = 1.8f - trailIndex * .18f;
                using var trailBrush = new SolidBrush(Color.FromArgb(Math.Max(0, alpha), _theme.Network));
                g.FillEllipse(trailBrush, trailPoint.X - radius, trailPoint.Y - radius, radius * 2, radius * 2);
            }
            using var packetGlow = new SolidBrush(Color.FromArgb((int)(visibility * 48), _theme.Network));
            using var packetCore = new SolidBrush(Color.FromArgb((int)(visibility * 220), _theme.Accent));
            g.FillEllipse(packetGlow, packet.X - 5, packet.Y - 5, 10, 10);
            g.FillEllipse(packetCore, packet.X - 2.2f, packet.Y - 2.2f, 4.4f, 4.4f);
        }
    }

    private static PointF LinearPoint(PointF start, PointF end, float amount)
    {
        return new PointF(
            start.X + (end.X - start.X) * amount,
            start.Y + (end.Y - start.Y) * amount);
    }

    private static void DrawDevice(Graphics g, int index, PointF p, Pen pen)
    {
        if (index == 1)
        {
            g.DrawRectangle(pen, p.X - 9, p.Y - 7, 18, 12);
            g.DrawLine(pen, p.X, p.Y + 5, p.X, p.Y + 9);
            g.DrawLine(pen, p.X - 5, p.Y + 9, p.X + 5, p.Y + 9);
        }
        else if (index == 2)
        {
            g.DrawRectangle(pen, p.X - 10, p.Y - 8, 20, 7);
            g.DrawRectangle(pen, p.X - 10, p.Y + 2, 20, 7);
        }
        else if (index == 3)
        {
            g.DrawRectangle(pen, p.X - 10, p.Y - 8, 20, 16);
            g.DrawLine(pen, p.X - 6, p.Y - 3, p.X + 6, p.Y - 3);
            g.DrawLine(pen, p.X + 6, p.Y + 3, p.X - 6, p.Y + 3);
        }
        else
        {
            g.DrawEllipse(pen, p.X - 10, p.Y - 10, 20, 20);
            g.DrawEllipse(pen, p.X - 5, p.Y - 10, 10, 20);
            g.DrawLine(pen, p.X - 9, p.Y, p.X + 9, p.Y);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pingTimer.Stop();
            _animationTimer.Stop();
            _pingTimer.Dispose();
            _animationTimer.Dispose();
            _toolTips.Dispose();
        }
        base.Dispose(disposing);
    }
}
