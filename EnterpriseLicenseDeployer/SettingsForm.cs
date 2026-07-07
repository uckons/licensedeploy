using System;
using System.Drawing;
using System.Windows.Forms;
using EnterpriseLicenseDeployer.Models;

namespace EnterpriseLicenseDeployer
{
    public partial class SettingsForm : Form
    {
        private readonly AppConfig _originalConfig;
        public AppConfig ResultConfig { get; private set; } = new();

        private TextBox _txtTargetIp = null!;
        private TextBox _txtLicenseFolder = null!;
        private TextBox _txtLogFolder = null!;
        private NumericUpDown _numCloseHour = null!;
        private NumericUpDown _numCloseMinute = null!;
        private NumericUpDown _numHour = null!;
        private NumericUpDown _numMinute = null!;

        private readonly TextBox[] _destinationBoxes = new TextBox[AppConfig.DestinationFolderCount];
        private readonly TextBox[] _applicationBoxes = new TextBox[AppConfig.ApplicationCount];

        public SettingsForm(AppConfig currentConfig)
        {
            _originalConfig = currentConfig;
            InitializeComponent();
            PopulateFromConfig(currentConfig);
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Text = "Settings";
            Size = new Size(760, 720);
            MinimumSize = new Size(760, 520);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            Font = new Font("Segoe UI", 9F);

            var rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                AutoScroll = true,
                Padding = new Padding(16)
            };

            var layout = new TableLayoutPanel
            {
                ColumnCount = 3,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Location = new Point(0, 0)
            };
            // Fixed (Absolute) widths only - mixing AutoSize with a Percent column is
            // unreliable and was causing the textbox column to render too narrow.
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));  // captions
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 400));  // textboxes (widened)
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));   // browse buttons

            int row = 0;

            void AddSectionHeader(string text)
            {
                var lbl = new Label
                {
                    Text = text,
                    Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(21, 41, 66),
                    AutoSize = true,
                    Margin = new Padding(0, 16, 0, 6)
                };
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.Controls.Add(lbl, 0, row);
                layout.SetColumnSpan(lbl, 3);
                row++;
            }

            (TextBox box, Button browse) AddPathRow(string caption, bool isFolder)
            {
                var lbl = new Label { Text = caption, AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 6, 8, 6) };
                var box = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 3, 6, 3) };
                var browseBtn = new Button { Text = "Browse...", Width = 80, Margin = new Padding(0, 3, 0, 3) };

                browseBtn.Click += (s, e) =>
                {
                    if (isFolder)
                    {
                        using var dlg = new FolderBrowserDialog();
                        if (!string.IsNullOrWhiteSpace(box.Text) && System.IO.Directory.Exists(box.Text))
                            dlg.SelectedPath = box.Text;
                        if (dlg.ShowDialog(this) == DialogResult.OK)
                            box.Text = dlg.SelectedPath;
                    }
                    else
                    {
                        using var dlg = new OpenFileDialog { Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*" };
                        if (dlg.ShowDialog(this) == DialogResult.OK)
                            box.Text = dlg.FileName;
                    }
                };

                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                layout.Controls.Add(lbl, 0, row);
                layout.Controls.Add(box, 1, row);
                layout.Controls.Add(browseBtn, 2, row);
                row++;

                return (box, browseBtn);
            }

            // --- General ---
            AddSectionHeader("General");

            var lblIp = new Label { Text = "Required Active IP", AutoSize = true, Margin = new Padding(0, 6, 8, 6) };
            _txtTargetIp = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 3, 6, 3) };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(lblIp, 0, row);
            layout.Controls.Add(_txtTargetIp, 1, row);
            row++;

            (_txtLicenseFolder, _) = AddPathRow("License Root Folder", isFolder: true);
            (_txtLogFolder, _) = AddPathRow("Log Folder", isFolder: true);

            // --- Close Apps Schedule ---
            AddSectionHeader("Daily Close Apps Schedule");

            var lblCloseTime = new Label { Text = "Close time (HH : MM)", AutoSize = true, Margin = new Padding(0, 6, 8, 6) };
            var closeTimePanel = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 3, 0, 3) };
            _numCloseHour = new NumericUpDown { Minimum = 0, Maximum = 23, Width = 60 };
            _numCloseMinute = new NumericUpDown { Minimum = 0, Maximum = 59, Width = 60 };
            closeTimePanel.Controls.Add(_numCloseHour);
            closeTimePanel.Controls.Add(new Label { Text = " : ", AutoSize = true, TextAlign = ContentAlignment.MiddleCenter, Padding = new Padding(4, 6, 4, 0) });
            closeTimePanel.Controls.Add(_numCloseMinute);
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(lblCloseTime, 0, row);
            layout.Controls.Add(closeTimePanel, 1, row);
            row++;

            // --- Schedule ---
            AddSectionHeader("Daily Recheck Schedule");

            var lblTime = new Label { Text = "Run time (HH : MM)", AutoSize = true, Margin = new Padding(0, 6, 8, 6) };
            var timePanel = new FlowLayoutPanel { AutoSize = true, Margin = new Padding(0, 3, 0, 3) };
            _numHour = new NumericUpDown { Minimum = 0, Maximum = 23, Width = 60 };
            _numMinute = new NumericUpDown { Minimum = 0, Maximum = 59, Width = 60 };
            timePanel.Controls.Add(_numHour);
            timePanel.Controls.Add(new Label { Text = " : ", AutoSize = true, TextAlign = ContentAlignment.MiddleCenter, Padding = new Padding(4, 6, 4, 0) });
            timePanel.Controls.Add(_numMinute);
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(lblTime, 0, row);
            layout.Controls.Add(timePanel, 1, row);
            row++;

            // --- Destination folders ---
            AddSectionHeader("Destination Folders (7)");
            for (int i = 0; i < AppConfig.DestinationFolderCount; i++)
            {
                var (box, _) = AddPathRow($"Destination Folder {i + 1}", isFolder: true);
                _destinationBoxes[i] = box;
            }

            // --- Applications ---
            AddSectionHeader("Applications (7)");
            for (int i = 0; i < AppConfig.ApplicationCount; i++)
            {
                var (box, _) = AddPathRow($"Application {i + 1}", isFolder: false);
                _applicationBoxes[i] = box;
            }

            // --- Bottom buttons ---
            var bottomPanel = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty, Padding = new Padding(12) };
            var btnSave = new Button
            {
                Text = "Save",
                Size = new Size(110, 32),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            var btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(110, 32),
                DialogResult = DialogResult.Cancel
            };

            btnSave.Location = new Point(bottomPanel.Width - 240, 10);
            btnCancel.Location = new Point(bottomPanel.Width - 120, 10);
            btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            bottomPanel.Controls.Add(btnSave);
            bottomPanel.Controls.Add(btnCancel);

            scrollPanel.Controls.Add(layout);
            rootLayout.Controls.Add(scrollPanel, 0, 0);
            rootLayout.Controls.Add(bottomPanel, 0, 1);
            Controls.Add(rootLayout);

            AcceptButton = btnSave;
            CancelButton = btnCancel;

            ResumeLayout(false);
        }

        private void PopulateFromConfig(AppConfig config)
        {
            _txtTargetIp.Text = config.TargetIp;
            _txtLicenseFolder.Text = config.LicenseFolderPath;
            _txtLogFolder.Text = config.LogFolderPath;
            _numCloseHour.Value = Math.Max(_numCloseHour.Minimum, Math.Min(_numCloseHour.Maximum, config.CloseAppsHour));
            _numCloseMinute.Value = Math.Max(_numCloseMinute.Minimum, Math.Min(_numCloseMinute.Maximum, config.CloseAppsMinute));
            _numHour.Value = Math.Max(_numHour.Minimum, Math.Min(_numHour.Maximum, config.ScheduledHour));
            _numMinute.Value = Math.Max(_numMinute.Minimum, Math.Min(_numMinute.Maximum, config.ScheduledMinute));

            for (int i = 0; i < AppConfig.DestinationFolderCount; i++)
                _destinationBoxes[i].Text = i < config.DestinationFolders.Count ? config.DestinationFolders[i] : string.Empty;

            for (int i = 0; i < AppConfig.ApplicationCount; i++)
                _applicationBoxes[i].Text = i < config.ApplicationPaths.Count ? config.ApplicationPaths[i] : string.Empty;
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtTargetIp.Text))
            {
                MessageBox.Show("Required Active IP cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            if (string.IsNullOrWhiteSpace(_txtLicenseFolder.Text))
            {
                MessageBox.Show("License Root Folder cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            if (string.IsNullOrWhiteSpace(_txtLogFolder.Text))
            {
                MessageBox.Show("Log Folder cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            var config = new AppConfig
            {
                TargetIp = _txtTargetIp.Text.Trim(),
                LicenseFolderPath = _txtLicenseFolder.Text.Trim(),
                LogFolderPath = _txtLogFolder.Text.Trim(),
                CloseAppsHour = (int)_numCloseHour.Value,
                CloseAppsMinute = (int)_numCloseMinute.Value,
                ScheduledHour = (int)_numHour.Value,
                ScheduledMinute = (int)_numMinute.Value
            };

            config.DestinationFolders.Clear();
            foreach (var box in _destinationBoxes)
                config.DestinationFolders.Add(box.Text.Trim());

            config.ApplicationPaths.Clear();
            foreach (var box in _applicationBoxes)
                config.ApplicationPaths.Add(box.Text.Trim());

            config.EnsureListSizes();
            ResultConfig = config;
        }
    }
}
