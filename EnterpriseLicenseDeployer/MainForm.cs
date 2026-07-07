using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using EnterpriseLicenseDeployer.Models;
using EnterpriseLicenseDeployer.Services;

namespace EnterpriseLicenseDeployer
{
    public partial class MainForm : Form
    {
        // ---- Palette (enterprise look) ----
        private static readonly Color HeaderColor = Color.FromArgb(21, 41, 66);      // deep navy
        private static readonly Color AccentColor = Color.FromArgb(0, 120, 215);      // corporate blue
        private static readonly Color BackColorMain = Color.FromArgb(245, 246, 248);  // light gray
        private static readonly Color OkColor = Color.FromArgb(16, 124, 16);
        private static readonly Color WarnColor = Color.FromArgb(196, 43, 28);

        private readonly ConfigService _configService = new();
        private readonly ScheduleService _scheduleService = new();
        private readonly DeploymentOrchestrator _orchestrator = new();

        private AppConfig _config = new();
        private System.Windows.Forms.Timer _clockTimer = null!;
        private DateTime _nextRunTime;
        private DateTime? _lastRunDate;

        // Status value labels (the "boxes")
        private Label _lblIpValue = null!;
        private Label _lblMacValue = null!;
        private Label _lblMatchValue = null!;
        private Label _lblCurrentTimeValue = null!;
        private Label _lblNextRunValue = null!;
        private TextBox _txtAuditLog = null!;
        private StatusStrip _statusStrip = null!;
        private ToolStripStatusLabel _statusLabel = null!;
        private Button _btnRunNow = null!;

        public MainForm()
        {
            InitializeComponent();
            LoadConfiguration();
            AuditLogger.Instance.LineWritten += OnAuditLineWritten;
            RecalculateNextRunTime();

            // Kick off an initial detection pass at startup (display only, no copy)
            RefreshDetectionDisplay();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "SKY License Deployment Manager";
            Size = new Size(920, 640);
            MinimumSize = new Size(820, 560);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = BackColorMain;
            Font = new Font("Segoe UI", 9F);
            Icon = SystemIcons.Application;

            // ---- Menu ----
            var menuStrip = new MenuStrip { BackColor = HeaderColor, ForeColor = Color.White };
            var fileMenu = new ToolStripMenuItem("File") { ForeColor = Color.Black };
            var settingsItem = new ToolStripMenuItem("Settings...", null, SettingsMenuItem_Click);
            var openLogItem = new ToolStripMenuItem("Open Log Folder", null, OpenLogMenuItem_Click);
            var exitItem = new ToolStripMenuItem("Exit", null, ExitMenuItem_Click);
            fileMenu.DropDownItems.Add(settingsItem);
            fileMenu.DropDownItems.Add(openLogItem);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(exitItem);
            menuStrip.Items.Add(fileMenu);
            MainMenuStrip = menuStrip;
            Controls.Add(menuStrip);

            // ---- Header banner ----
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = HeaderColor
            };
            var titleLabel = new Label
            {
                Text = "SKY License Deployment Manager",
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 16)
            };
            var subtitleLabel = new Label
            {
                Text = "Automated MAC-based license provisioning",
                ForeColor = Color.FromArgb(190, 205, 220),
                Font = new Font("Segoe UI", 9F),
                AutoSize = true,
                Location = new Point(23, 42)
            };
            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(subtitleLabel);
            Controls.Add(headerPanel);

            // ---- Status group box ----
            var statusGroup = new GroupBox
            {
                Text = "System Status",
                Dock = DockStyle.Top,
                Height = 220,
                Padding = new Padding(16),
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold)
            };

            var grid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 5,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 5; i++) grid.RowStyles.Add(new RowStyle(SizeType.Percent, 20));

            (Label caption, Label value) MakeRow(string caption)
            {
                var capLabel = new Label
                {
                    Text = caption,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                    ForeColor = Color.FromArgb(90, 90, 90),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                var valLabel = new Label
                {
                    Text = "-",
                    Font = new Font("Consolas", 11F, FontStyle.Bold),
                    ForeColor = Color.Black,
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(8, 0, 0, 0),
                    Margin = new Padding(0, 4, 0, 4)
                };
                return (capLabel, valLabel);
            }

            var (c1, v1) = MakeRow("Active IP Address");
            var (c2, v2) = MakeRow("Active MAC Address");
            var (c3, v3) = MakeRow("License Match Status");
            var (c4, v4) = MakeRow("Current Time");
            var (c5, v5) = MakeRow("Next Scheduled Run (06:50 daily)");

            _lblIpValue = v1; _lblMacValue = v2; _lblMatchValue = v3;
            _lblCurrentTimeValue = v4; _lblNextRunValue = v5;

            grid.Controls.Add(c1, 0, 0); grid.Controls.Add(v1, 1, 0);
            grid.Controls.Add(c2, 0, 1); grid.Controls.Add(v2, 1, 1);
            grid.Controls.Add(c3, 0, 2); grid.Controls.Add(v3, 1, 2);
            grid.Controls.Add(c4, 0, 3); grid.Controls.Add(v4, 1, 3);
            grid.Controls.Add(c5, 0, 4); grid.Controls.Add(v5, 1, 4);

            statusGroup.Controls.Add(grid);
            Controls.Add(statusGroup);

            // ---- Button bar ----
            var buttonPanel = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(16, 8, 16, 8) };
            _btnRunNow = MakeButton("Run Now", AccentColor, BtnRunNow_Click);
            var btnSettings = MakeButton("Settings", Color.FromArgb(90, 98, 108), BtnSettings_Click);
            var btnOpenLogFolder = MakeButton("Open Log Folder", Color.FromArgb(90, 98, 108), BtnOpenLogFolder_Click);
            var btnExit = MakeButton("Exit", Color.FromArgb(160, 40, 40), BtnExit_Click);

            _btnRunNow.Location = new Point(0, 8);
            btnSettings.Location = new Point(150, 8);
            btnOpenLogFolder.Location = new Point(300, 8);
            btnExit.Location = new Point(470, 8);

            buttonPanel.Controls.Add(_btnRunNow);
            buttonPanel.Controls.Add(btnSettings);
            buttonPanel.Controls.Add(btnOpenLogFolder);
            buttonPanel.Controls.Add(btnExit);
            Controls.Add(buttonPanel);

            // ---- Status strip (must be added before the Fill panel so it keeps its space) ----
            _statusStrip = new StatusStrip();
            _statusLabel = new ToolStripStatusLabel("Ready") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            _statusStrip.Items.Add(_statusLabel);
            Controls.Add(_statusStrip);

            // ---- Audit log (Fill - must be added last so it consumes remaining space) ----
            var logGroup = new GroupBox
            {
                Text = "Audit Log (live)",
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold)
            };
            _txtAuditLog = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9F),
                BackColor = Color.White,
                WordWrap = false
            };
            logGroup.Controls.Add(_txtAuditLog);
            Controls.Add(logGroup);

            // ---- Clock / scheduler timer ----
            _clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _clockTimer.Tick += ClockTimer_Tick;
            _clockTimer.Start();

            ResumeLayout(false);
            PerformLayout();
        }

        // Named handlers (Designer-friendly: avoids lambdas inside InitializeComponent)
        private void SettingsMenuItem_Click(object? sender, EventArgs e) => OpenSettings();
        private void OpenLogMenuItem_Click(object? sender, EventArgs e) => OpenLogFolder();
        private void ExitMenuItem_Click(object? sender, EventArgs e) => Close();
        private void BtnRunNow_Click(object? sender, EventArgs e) => RunRoutineManually();
        private void BtnSettings_Click(object? sender, EventArgs e) => OpenSettings();
        private void BtnOpenLogFolder_Click(object? sender, EventArgs e) => OpenLogFolder();
        private void BtnExit_Click(object? sender, EventArgs e) => Close();

        private Button MakeButton(string text, Color color, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(140, 36),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += onClick;
            return btn;
        }

        // ---------------------------------------------------------------
        // Configuration
        // ---------------------------------------------------------------

        private void LoadConfiguration()
        {
            _config = _configService.Load();
            _lblNextRunValue.Text = $"{_config.ScheduledHour:D2}:{_config.ScheduledMinute:D2} (calculating...)";
        }

        private void OpenSettings()
        {
            using var form = new SettingsForm(_config);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                _config = form.ResultConfig;
                _configService.Save(_config);
                RecalculateNextRunTime();
                RefreshDetectionDisplay();
            }
        }

        private void OpenLogFolder()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = AuditLogger.Instance.LogDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open log folder: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ---------------------------------------------------------------
        // Detection / routine execution
        // ---------------------------------------------------------------

        private void RefreshDetectionDisplay()
        {
            try
            {
                var netService = new NetworkService();
                var info = netService.GetActiveNetworkInfo();

                if (info == null)
                {
                    _lblIpValue.Text = "(no active adapter)";
                    _lblMacValue.Text = "(no active adapter)";
                    SetMatchStatus("Unknown", WarnColor);
                    return;
                }

                _lblIpValue.Text = info.IpAddress;
                _lblMacValue.Text = info.MacAddress;

                bool ipMatches = string.Equals(info.IpAddress, _config.TargetIp, StringComparison.OrdinalIgnoreCase);
                if (!ipMatches)
                {
                    SetMatchStatus($"IP mismatch (expected {_config.TargetIp})", WarnColor);
                    return;
                }

                var licenseService = new LicenseService();
                var folder = licenseService.FindMatchingLicenseFolder(_config.LicenseFolderPath, info.MacAddress);
                SetMatchStatus(folder != null ? "License folder matched" : "No matching license folder",
                    folder != null ? OkColor : WarnColor);
            }
            catch (Exception ex)
            {
                AuditLogger.Instance.Log("ERROR", $"Detection display failed: {ex.Message}");
            }
        }

        private void SetMatchStatus(string text, Color color)
        {
            _lblMatchValue.Text = text;
            _lblMatchValue.ForeColor = color;
        }

        private void RunRoutineManually()
        {
            _btnRunNow.Enabled = false;
            _statusLabel.Text = "Running deployment routine...";
            try
            {
                var result = _orchestrator.Execute(_config);
                RefreshDetectionDisplay();
                _statusLabel.Text = result.Message;

                MessageBox.Show(result.Message, result.Success ? "Success" : "Attention",
                    MessageBoxButtons.OK, result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            finally
            {
                _btnRunNow.Enabled = true;
            }
        }

        // ---------------------------------------------------------------
        // Clock + daily scheduler
        // ---------------------------------------------------------------

        private void RecalculateNextRunTime()
        {
            _nextRunTime = _scheduleService.GetNextRunTime(_config.ScheduledHour, _config.ScheduledMinute, DateTime.Now);
            _lblNextRunValue.Text = _nextRunTime.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            _lblCurrentTimeValue.Text = now.ToString("yyyy-MM-dd HH:mm:ss");

            // Fire once when we reach/pass the scheduled minute, and not already run today.
            if (now >= _nextRunTime && (_lastRunDate == null || _lastRunDate.Value.Date != now.Date))
            {
                _lastRunDate = now.Date;
                AuditLogger.Instance.Log("INFO", "Scheduled recheck triggered.");
                _statusLabel.Text = "Scheduled recheck running...";

                var result = _orchestrator.Execute(_config);
                RefreshDetectionDisplay();
                _statusLabel.Text = result.Message;

                RecalculateNextRunTime();
            }
        }

        private void OnAuditLineWritten(string line)
        {
            if (_txtAuditLog.IsDisposed) return;

            if (_txtAuditLog.InvokeRequired)
            {
                _txtAuditLog.BeginInvoke(new Action(() => AppendLogLine(line)));
            }
            else
            {
                AppendLogLine(line);
            }
        }

        private void AppendLogLine(string line)
        {
            _txtAuditLog.AppendText(line + Environment.NewLine);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            AuditLogger.Instance.LineWritten -= OnAuditLineWritten;
            _clockTimer?.Stop();
            _clockTimer?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
