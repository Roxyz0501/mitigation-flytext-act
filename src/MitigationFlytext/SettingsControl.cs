using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace MitigationFlytext
{
    public sealed class SettingsControl : UserControl
    {
        public const string SupportUrl = "https://ko-fi.com/roxyz0501";
        private readonly ComboBox language = new ComboBox();
        private readonly CheckBox enabled = new CheckBox(), locked = new CheckBox(), preview = new CheckBox(), checkUpdates = new CheckBox();
        private readonly NumericUpDown duration = Number(), lines = Number(), opacity = Number(), fontSize = Number();
        private readonly TabControl tabs = new TabControl();
        private readonly Label subtitle = new Label(), hint = new Label(), estimate = new Label(), updateStatus = new Label();
        private readonly Button supportButton = new Button(), checkButton = new Button(), updateButton = new Button(), laterButton = new Button();
        private readonly Label[] captions = { new Label(), new Label(), new Label(), new Label(), new Label() };
        private PluginSettings settings;
        private bool loading;
        public event EventHandler SettingsChanged;
        public event EventHandler CheckUpdatesRequested;
        public event EventHandler InstallUpdateRequested;
        public event EventHandler LaterRequested;
        internal ReleaseInfo AvailableRelease { get; private set; }
        public SettingsControl(PluginSettings value)
        {
            settings = value; Dock = DockStyle.Fill; AutoScroll = true; BackColor = Color.FromArgb(244, 246, 249); ForeColor = Color.FromArgb(45, 56, 72);
            Build(); loading = true; LoadValues(); loading = false; ApplyLanguage();
        }
        private void Build()
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 76, BackColor = Color.FromArgb(37, 48, 65) };
            var title = new Label { Text = "Mitigation Flytext", ForeColor = Color.White, Font = new Font("Segoe UI", 18f, FontStyle.Bold), AutoSize = true, Location = new Point(20, 11) };
            subtitle.AutoSize = true; subtitle.ForeColor = Color.FromArgb(202, 211, 224); subtitle.Location = new Point(22, 47); header.Controls.Add(title); header.Controls.Add(subtitle);
            tabs.Dock = DockStyle.Fill; var general = new TabPage(); var support = new TabPage(); tabs.TabPages.Add(general); tabs.TabPages.Add(support);
            var grid = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(20), ColumnCount = 2 };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240)); grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); general.Controls.Add(grid);
            language.DropDownStyle = ComboBoxStyle.DropDownList; language.Items.AddRange(new object[] { "English", "日本語", "简体中文", "한국어" });
            Add(grid, captions[0], language, 0); AddWide(grid, enabled, 1); AddWide(grid, locked, 2); AddWide(grid, preview, 3);
            duration.Minimum = 1200; duration.Maximum = 10000; duration.Increment = 100; Add(grid, captions[1], duration, 4);
            lines.Minimum = 1; lines.Maximum = 12; Add(grid, captions[2], lines, 5);
            opacity.Minimum = 30; opacity.Maximum = 100; Add(grid, captions[3], opacity, 6);
            fontSize.Minimum = 12; fontSize.Maximum = 40; Add(grid, captions[4], fontSize, 7);
            hint.AutoSize = true; hint.MaximumSize = new Size(720, 0); hint.ForeColor = Color.FromArgb(76, 91, 111); AddWide(grid, hint, 8);
            estimate.AutoSize = true; estimate.MaximumSize = new Size(720, 0); estimate.ForeColor = Color.FromArgb(164, 92, 34); AddWide(grid, estimate, 9);
            AddWide(grid, checkUpdates, 10); var updateFlow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill }; updateFlow.Controls.Add(checkButton); updateFlow.Controls.Add(updateButton); updateFlow.Controls.Add(laterButton); updateFlow.Controls.Add(updateStatus); AddWide(grid, updateFlow, 11); updateButton.Visible = false; laterButton.Visible = false;
            var supportLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(28), WrapContents = false, BackColor = Color.FromArgb(255, 249, 235) };
            var supportHeading = new Label { Name = "SupportHeading", AutoSize = true, Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.FromArgb(151, 86, 12), Margin = new Padding(0, 10, 0, 12) };
            var supportBody = new Label { Name = "SupportBody", AutoSize = true, MaximumSize = new Size(720, 0), Margin = new Padding(0, 0, 0, 18) };
            supportButton.AutoSize = true; supportButton.Padding = new Padding(18, 8, 18, 8); supportButton.BackColor = Color.FromArgb(231, 142, 26); supportButton.ForeColor = Color.White; supportButton.FlatStyle = FlatStyle.Flat;
            supportLayout.Controls.Add(supportHeading); supportLayout.Controls.Add(supportBody); supportLayout.Controls.Add(supportButton); support.Controls.Add(supportLayout);
            Controls.Add(tabs); Controls.Add(header);
            language.SelectedIndexChanged += Changed; enabled.CheckedChanged += Changed; locked.CheckedChanged += Changed; preview.CheckedChanged += Changed; duration.ValueChanged += Changed; lines.ValueChanged += Changed; opacity.ValueChanged += Changed; fontSize.ValueChanged += Changed; checkUpdates.CheckedChanged += Changed;
            supportButton.Click += delegate { string error; if (!TryOpenSupportLink(Process.Start, out error)) MessageBox.Show(this, Localization.Get(settings.Language, "LinkFailed", error)); };
            checkButton.Click += delegate { CheckUpdatesRequested?.Invoke(this, EventArgs.Empty); };
            updateButton.Click += delegate { InstallUpdateRequested?.Invoke(this, EventArgs.Empty); };
            laterButton.Click += delegate { LaterRequested?.Invoke(this, EventArgs.Empty); };
        }
        private void LoadValues() { language.SelectedIndex = settings.Language == "ja" ? 1 : settings.Language == "zh-CN" ? 2 : settings.Language == "ko" ? 3 : 0; enabled.Checked = settings.OverlayEnabled; locked.Checked = settings.Locked; preview.Checked = settings.Preview; duration.Value = settings.DurationMilliseconds; lines.Value = settings.MaximumLines; opacity.Value = settings.OpacityPercent; fontSize.Value = settings.FontSize; checkUpdates.Checked = settings.CheckUpdatesOnStartup; }
        private void Changed(object sender, EventArgs e) { if (loading) return; ApplyTo(settings); ApplyLanguage(); SettingsChanged?.Invoke(this, EventArgs.Empty); }
        public void ApplyTo(PluginSettings v) { v.Language = language.SelectedIndex == 1 ? "ja" : language.SelectedIndex == 2 ? "zh-CN" : language.SelectedIndex == 3 ? "ko" : "en"; v.OverlayEnabled = enabled.Checked; v.Locked = locked.Checked; v.Preview = preview.Checked; v.DurationMilliseconds = (int)duration.Value; v.MaximumLines = (int)lines.Value; v.OpacityPercent = (int)opacity.Value; v.FontSize = (int)fontSize.Value; v.CheckUpdatesOnStartup = checkUpdates.Checked; }
        private void ApplyLanguage() { Font = new Font(Localization.FontFamily(settings.Language), 9f); tabs.TabPages[0].Text = L("Settings"); tabs.TabPages[1].Text = L("Support"); subtitle.Text = L("Subtitle"); captions[0].Text = L("Language"); captions[1].Text = L("Duration"); captions[2].Text = L("Lines"); captions[3].Text = L("Opacity"); captions[4].Text = L("FontSize"); enabled.Text = L("Enabled"); locked.Text = L("Locked"); preview.Text = L("Preview"); hint.Text = L("Hint"); estimate.Text = L("EstimateNote"); checkUpdates.Text = L("CheckStartup"); checkButton.Text = L("CheckNow"); updateButton.Text = L("UpdateNow"); laterButton.Text = L("Later"); Find("SupportHeading").Text = L("SupportHeading"); Find("SupportBody").Text = L("SupportBody"); supportButton.Text = L("SupportButton"); }
        internal void SetUpdateChecking() { updateStatus.Text = L("Checking"); updateButton.Visible = false; laterButton.Visible = false; }
        internal void ShowUpdateResult(UpdateCheckResult result, Version current)
        {
            AvailableRelease = result == null ? null : result.Release; updateButton.Visible = result != null && result.Status == UpdateCheckStatus.UpdateAvailable; laterButton.Visible = updateButton.Visible;
            if (result == null || result.Status == UpdateCheckStatus.Failed || result.Status == UpdateCheckStatus.RepositoryNotConfigured) updateStatus.Text = L("CheckFailed");
            else if (result.Status == UpdateCheckStatus.NoStableRelease) updateStatus.Text = L("NoRelease");
            else if (result.Status == UpdateCheckStatus.UpToDate) updateStatus.Text = Localization.Get(settings.Language, "UpToDate", current.ToString(3));
            else updateStatus.Text = Localization.Get(settings.Language, "UpdateAvailable", current.ToString(3), result.Release.Version, result.Release.Notes);
        }
        internal void SetUpdateText(string key, string detail = null) { updateStatus.Text = detail == null ? L(key) : Localization.Get(settings.Language, key, detail); if (key == "Prepared" || key == "UpdateFailed") { updateButton.Visible = false; laterButton.Visible = false; } }
        private Control Find(string name) { Control[] result = Controls.Find(name, true); return result.Length == 0 ? this : result[0]; }
        private string L(string key) => Localization.Get(settings.Language, key);
        private static NumericUpDown Number() => new NumericUpDown { Width = 120, ThousandsSeparator = true };
        private static void Add(TableLayoutPanel p, Control a, Control b, int row) { a.AutoSize = true; a.Anchor = AnchorStyles.Left; b.Anchor = AnchorStyles.Left; p.Controls.Add(a, 0, row); p.Controls.Add(b, 1, row); }
        private static void AddWide(TableLayoutPanel p, Control c, int row) { c.Anchor = AnchorStyles.Left; p.Controls.Add(c, 0, row); p.SetColumnSpan(c, 2); }
        public static bool TryOpenSupportLink(Func<ProcessStartInfo, Process> opener, out string error) { try { opener(new ProcessStartInfo(SupportUrl) { UseShellExecute = true }); error = null; return true; } catch (Exception ex) { error = ex.Message; return false; } }
    }
}
