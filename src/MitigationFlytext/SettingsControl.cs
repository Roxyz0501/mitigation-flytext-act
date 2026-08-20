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
        private readonly PluginSettings settings;
        private readonly Label headerTitle = L(), headerSubtitle = L();
        private readonly TabControl tabs = new TabControl();
        private readonly TabPage settingsTab = new TabPage(), updateTab = new TabPage(), supportTab = new TabPage();
        private readonly ComboBox language = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        private readonly CheckBox enabled = C(), locked = C(), preview = C(), checkUpdates = C();
        private readonly NumericUpDown duration = N(1200, 10000, 100), lines = N(1, 12, 1), fontSize = N(12, 40, 1);
        private readonly TrackBar opacity = new TrackBar { Minimum = 30, Maximum = 100, TickFrequency = 5, AutoSize = false, Height = 36, Width = 190 };
        private readonly Label languageLabel = L(), durationLabel = L(), linesLabel = L(), opacityLabel = L(), opacityValue = L(), fontSizeLabel = L();
        private readonly Label hint = L(), estimate = L(), versionLabel = L();
        private readonly Label updateTitle = L(), updateDescription = L(), currentCaption = L(), currentValue = L(), latestCaption = L(), latestValue = L(), updateStatus = L(), releaseCaption = L();
        private readonly TextBox releaseNotes = new TextBox();
        private readonly Button checkButton = new Button(), updateButton = new Button(), laterButton = new Button();
        private readonly Label supportHeading = L(), supportDescription = L(), supportOptional = L(), supportUrl = L(), supportSafety = L(), supportStatus = L();
        private readonly Button supportButton = new Button();
        private bool loading = true;
        private int hoveredTabIndex = -1;
        private string updateMessageKey = "NotChecked";
        private object[] updateMessageArgs = new object[0];

        public event EventHandler SettingsChanged;
        public event EventHandler CheckUpdatesRequested;
        public event EventHandler InstallUpdateRequested;
        public event EventHandler LaterRequested;
        internal ReleaseInfo AvailableRelease { get; private set; }

        public SettingsControl(PluginSettings value)
        {
            settings = value; Dock = DockStyle.Fill; BackColor = Color.FromArgb(244, 247, 251); Padding = new Padding(14);
            language.Items.AddRange(new object[] { "English", "日本語", "简体中文", "한국어" }); language.SelectedIndex = LanguageIndex(settings.Language);
            enabled.Checked = settings.OverlayEnabled; locked.Checked = settings.Locked; preview.Checked = settings.Preview;
            duration.Value = Bound(duration, settings.DurationMilliseconds); lines.Value = Bound(lines, settings.MaximumLines);
            opacity.Value = Math.Max(opacity.Minimum, Math.Min(opacity.Maximum, settings.OpacityPercent)); opacityValue.Text = opacity.Value + "%";
            fontSize.Value = Bound(fontSize, settings.FontSize); checkUpdates.Checked = settings.CheckUpdatesOnStartup;

            var header = BuildHeader(); ConfigureTabs();
            settingsTab.Controls.Add(BuildSettingsLayout()); updateTab.Controls.Add(BuildUpdateLayout()); supportTab.Controls.Add(BuildSupportLayout());
            tabs.TabPages.Add(settingsTab); tabs.TabPages.Add(updateTab); tabs.TabPages.Add(supportTab);
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = BackColor };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(header, 0, 0); root.Controls.Add(tabs, 0, 1); Controls.Add(root);
            versionLabel.Text = "v" + Assembly.GetExecutingAssembly().GetName().Version.ToString(3); versionLabel.Font = new Font("Segoe UI", 8f); versionLabel.ForeColor = Color.FromArgb(113, 126, 146); versionLabel.BackColor = Color.Transparent;
            Controls.Add(versionLabel); PositionVersionLabel(); WireEvents(); ApplyLanguage(); loading = false;
        }

        private Control BuildHeader()
        {
            var header = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(18, 11, 18, 8) };
            headerTitle.Location = new Point(18, 9); headerTitle.ForeColor = Color.FromArgb(34, 44, 61);
            headerSubtitle.Location = new Point(20, 43); headerSubtitle.ForeColor = Color.FromArgb(103, 116, 137);
            header.Paint += delegate(object sender, PaintEventArgs e) { using (var pen = new Pen(Color.FromArgb(202, 165, 87), 2f)) e.Graphics.DrawLine(pen, 0, header.Height - 2, header.Width, header.Height - 2); };
            header.Controls.Add(headerTitle); header.Controls.Add(headerSubtitle); return header;
        }

        private void ConfigureTabs()
        {
            tabs.Dock = DockStyle.Fill; tabs.DrawMode = TabDrawMode.OwnerDrawFixed; tabs.ItemSize = new Size(118, 31); tabs.SizeMode = TabSizeMode.Fixed; tabs.Padding = new Point(18, 5);
            settingsTab.BackColor = updateTab.BackColor = Color.White; supportTab.BackColor = Color.FromArgb(255, 249, 239);
            settingsTab.Padding = updateTab.Padding = supportTab.Padding = new Padding(4);
            tabs.DrawItem += DrawTab; tabs.MouseMove += TabsMouseMove; tabs.MouseLeave += delegate { hoveredTabIndex = -1; tabs.Invalidate(); };
        }

        private Control BuildSettingsLayout()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, AutoScroll = true };
            var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, RowCount = 10, Padding = new Padding(24, 20, 24, 20), BackColor = Color.White };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 235)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            var heights = new[] { 42, 34, 34, 34, 42, 42, 46, 42, 62, 78 }; foreach (var h in heights) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, h));
            layout.Controls.Add(languageLabel, 0, 0); layout.Controls.Add(language, 1, 0);
            Wide(layout, enabled, 1); Wide(layout, locked, 2); Wide(layout, preview, 3);
            layout.Controls.Add(durationLabel, 0, 4); layout.Controls.Add(duration, 1, 4); layout.Controls.Add(linesLabel, 0, 5); layout.Controls.Add(lines, 1, 5);
            layout.Controls.Add(opacityLabel, 0, 6); layout.Controls.Add(SliderPanel(opacity, opacityValue), 1, 6); layout.Controls.Add(fontSizeLabel, 0, 7); layout.Controls.Add(fontSize, 1, 7);
            hint.AutoSize = false; hint.Dock = DockStyle.Fill; hint.ForeColor = Color.FromArgb(73, 96, 128); hint.Padding = new Padding(0, 8, 0, 0); Wide(layout, hint, 8);
            estimate.AutoSize = false; estimate.Dock = DockStyle.Fill; estimate.ForeColor = Color.FromArgb(151, 91, 29); estimate.Padding = new Padding(0, 5, 0, 0); Wide(layout, estimate, 9);
            panel.Controls.Add(layout); return panel;
        }

        private Control BuildUpdateLayout()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, AutoScroll = true };
            var layout = new TableLayoutPanel { Dock = DockStyle.Top, Height = 520, ColumnCount = 2, RowCount = 9, Padding = new Padding(24), BackColor = Color.White };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            foreach (var h in new[] { 44, 48, 42, 36, 36, 58, 30, 120, 58 }) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, h));
            updateTitle.AutoSize = false; updateTitle.Dock = DockStyle.Fill; updateTitle.ForeColor = Color.FromArgb(34, 44, 61); Wide(layout, updateTitle, 0);
            updateDescription.AutoSize = false; updateDescription.Dock = DockStyle.Fill; updateDescription.ForeColor = Color.FromArgb(73, 96, 128); Wide(layout, updateDescription, 1);
            layout.Controls.Add(checkUpdates, 0, 2); Secondary(checkButton); checkButton.Width = 150; checkButton.Height = 30; layout.Controls.Add(checkButton, 1, 2);
            layout.Controls.Add(currentCaption, 0, 3); currentValue.Text = Assembly.GetExecutingAssembly().GetName().Version.ToString(3); layout.Controls.Add(currentValue, 1, 3);
            layout.Controls.Add(latestCaption, 0, 4); latestValue.Text = "—"; layout.Controls.Add(latestValue, 1, 4);
            updateStatus.AutoSize = false; updateStatus.Dock = DockStyle.Fill; updateStatus.ForeColor = Color.FromArgb(73, 96, 128); Wide(layout, updateStatus, 5);
            Wide(layout, releaseCaption, 6); releaseNotes.Multiline = true; releaseNotes.ReadOnly = true; releaseNotes.ScrollBars = ScrollBars.Vertical; releaseNotes.Dock = DockStyle.Fill; releaseNotes.BackColor = Color.FromArgb(250, 251, 252); layout.Controls.Add(releaseNotes, 0, 7); layout.SetColumnSpan(releaseNotes, 2);
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = Padding.Empty };
            Primary(updateButton); updateButton.Width = 250; updateButton.Height = 40; Secondary(laterButton); laterButton.Width = 110; laterButton.Height = 40; updateButton.Enabled = laterButton.Enabled = false;
            buttons.Controls.Add(updateButton); buttons.Controls.Add(laterButton); layout.Controls.Add(buttons, 0, 8); layout.SetColumnSpan(buttons, 2); panel.Controls.Add(layout); return panel;
        }

        private Control BuildSupportLayout()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(255, 249, 239), Padding = new Padding(28), AutoScroll = true };
            var card = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, RowCount = 7, BackColor = Color.White, Padding = new Padding(28, 23, 28, 24) };
            supportHeading.ForeColor = Color.FromArgb(194, 112, 22); supportHeading.Margin = new Padding(0, 0, 0, 10); card.Controls.Add(supportHeading, 0, 0);
            supportDescription.MaximumSize = new Size(700, 0); supportDescription.ForeColor = Color.FromArgb(61, 68, 79); supportDescription.Margin = new Padding(0, 0, 0, 8); card.Controls.Add(supportDescription, 0, 1);
            supportOptional.MaximumSize = new Size(700, 0); supportOptional.ForeColor = Color.FromArgb(117, 76, 24); supportOptional.Margin = new Padding(0, 0, 0, 18); card.Controls.Add(supportOptional, 0, 2);
            supportButton.AutoSize = false; supportButton.Size = new Size(360, 50); Primary(supportButton); supportButton.Font = new Font("Yu Gothic UI", 11f, FontStyle.Bold); supportButton.Margin = new Padding(0, 8, 0, 8); card.Controls.Add(supportButton, 0, 3);
            supportUrl.Text = SupportUrl; supportUrl.ForeColor = Color.FromArgb(126, 104, 78); supportUrl.Margin = new Padding(1, 0, 0, 8); card.Controls.Add(supportUrl, 0, 4);
            supportSafety.ForeColor = Color.FromArgb(73, 96, 128); supportSafety.Margin = new Padding(0, 0, 0, 8); card.Controls.Add(supportSafety, 0, 5); supportStatus.ForeColor = Color.FromArgb(176, 65, 56); card.Controls.Add(supportStatus, 0, 6);
            panel.Controls.Add(card); return panel;
        }

        private void WireEvents()
        {
            language.SelectedIndexChanged += Changed; enabled.CheckedChanged += Changed; locked.CheckedChanged += Changed; preview.CheckedChanged += Changed; duration.ValueChanged += Changed; lines.ValueChanged += Changed;
            opacity.ValueChanged += delegate { opacityValue.Text = opacity.Value + "%"; Changed(this, EventArgs.Empty); }; fontSize.ValueChanged += Changed; checkUpdates.CheckedChanged += Changed;
            checkButton.Click += delegate { CheckUpdatesRequested?.Invoke(this, EventArgs.Empty); }; updateButton.Click += delegate { InstallUpdateRequested?.Invoke(this, EventArgs.Empty); }; laterButton.Click += delegate { LaterRequested?.Invoke(this, EventArgs.Empty); }; supportButton.Click += OpenSupportLink;
        }

        public void ApplyTo(PluginSettings value)
        {
            value.Language = CurrentLanguage; value.OverlayEnabled = enabled.Checked; value.Locked = locked.Checked; value.Preview = preview.Checked; value.DurationMilliseconds = (int)duration.Value;
            value.MaximumLines = (int)lines.Value; value.OpacityPercent = opacity.Value; value.FontSize = (int)fontSize.Value; value.CheckUpdatesOnStartup = checkUpdates.Checked; value.Normalize();
        }

        internal void SetUpdateChecking() { AvailableRelease = null; checkButton.Enabled = false; updateButton.Enabled = laterButton.Enabled = false; latestValue.Text = "—"; releaseNotes.Clear(); updateStatus.ForeColor = Color.FromArgb(73, 96, 128); SetUpdateMessage("Checking"); }
        internal void ShowUpdateResult(UpdateCheckResult result, Version current)
        {
            checkButton.Enabled = true; AvailableRelease = result == null ? null : result.Release; currentValue.Text = current.ToString(3); latestValue.Text = result?.Release?.Version?.ToString() ?? "—";
            releaseNotes.Text = string.IsNullOrWhiteSpace(result?.Release?.Notes) ? T("NoReleaseNotes") : result.Release.Notes; updateButton.Enabled = laterButton.Enabled = result != null && result.Status == UpdateCheckStatus.UpdateAvailable;
            if (result == null || result.Status == UpdateCheckStatus.Failed || result.Status == UpdateCheckStatus.RepositoryNotConfigured) { updateStatus.ForeColor = Color.FromArgb(176, 65, 56); SetUpdateMessage("CheckFailed"); }
            else if (result.Status == UpdateCheckStatus.NoStableRelease) { updateStatus.ForeColor = Color.FromArgb(151, 91, 29); SetUpdateMessage("NoRelease"); }
            else if (result.Status == UpdateCheckStatus.UpToDate) { updateStatus.ForeColor = Color.FromArgb(17, 108, 75); SetUpdateMessage("UpToDate", current.ToString(3)); }
            else { updateStatus.ForeColor = Color.FromArgb(176, 65, 56); SetUpdateMessage("UpdateAvailable", current.ToString(3), result.Release.Version); }
        }
        internal void SetUpdateText(string key, string detail = null) { updateStatus.ForeColor = key == "Prepared" ? Color.FromArgb(17, 108, 75) : key == "UpdateFailed" ? Color.FromArgb(176, 65, 56) : Color.FromArgb(73, 96, 128); SetUpdateMessage(key, detail == null ? new object[0] : new object[] { detail }); var done = key == "Prepared" || key == "UpdateFailed" || key == "Later"; if (done) updateButton.Enabled = laterButton.Enabled = false; checkButton.Enabled = key != "Preparing"; }
        internal void ShowUpdateSkipped(string version) { updateButton.Enabled = laterButton.Enabled = false; updateStatus.ForeColor = Color.FromArgb(73, 96, 128); SetUpdateMessage("UpdateSkipped", version); }
        internal void FocusUpdateTab() { tabs.SelectedTab = updateTab; }

        private void Changed(object sender, EventArgs e) { if (loading) return; ApplyTo(settings); ApplyLanguage(); SettingsChanged?.Invoke(this, EventArgs.Empty); }
        private void ApplyLanguage()
        {
            var family = Localization.FontFamily(CurrentLanguage); ApplyFontFamily(this, family);
            headerTitle.Text = T("PluginTab"); headerTitle.Font = new Font(family, 17f, FontStyle.Bold); headerSubtitle.Text = T("Subtitle"); headerSubtitle.Font = new Font(family, 8.5f);
            tabs.Font = new Font(family, 9f, FontStyle.Bold); settingsTab.Text = T("Settings"); updateTab.Text = T("Updates"); supportTab.Text = T("Support");
            languageLabel.Text = T("Language"); enabled.Text = T("Enabled"); locked.Text = T("Locked"); preview.Text = T("Preview"); durationLabel.Text = T("Duration"); linesLabel.Text = T("Lines"); opacityLabel.Text = T("Opacity"); fontSizeLabel.Text = T("FontSize"); hint.Text = T("Hint"); estimate.Text = T("EstimateNote");
            updateTitle.Text = T("UpdateTitle"); updateTitle.Font = new Font(family, 16f, FontStyle.Bold); updateDescription.Text = T("UpdateDescription"); checkUpdates.Text = T("CheckStartup"); checkButton.Text = T("CheckNow"); currentCaption.Text = T("CurrentVersion"); latestCaption.Text = T("LatestVersion"); releaseCaption.Text = T("ReleaseNotes"); updateButton.Text = T("UpdateNow"); laterButton.Text = T("Later");
            supportHeading.Text = T("SupportHeading"); supportHeading.Font = new Font(family, 18f, FontStyle.Bold); supportDescription.Text = T("SupportDescription"); supportOptional.Text = T("SupportBody"); supportOptional.Font = new Font(family, 9.5f, FontStyle.Bold); supportButton.Text = T("SupportButton"); supportSafety.Text = T("SupportSafety");
            if (string.IsNullOrWhiteSpace(releaseNotes.Text)) releaseNotes.Text = T("NoReleaseNotes"); RefreshUpdateMessage(); tabs.Invalidate(); Invalidate(true);
        }

        private void SetUpdateMessage(string key, params object[] args) { updateMessageKey = key; updateMessageArgs = args ?? new object[0]; RefreshUpdateMessage(); }
        private void RefreshUpdateMessage() { updateStatus.Text = T(updateMessageKey, updateMessageArgs); }
        private string T(string key, params object[] args) => Localization.Get(CurrentLanguage, key, args);
        private string CurrentLanguage => language.SelectedIndex == 1 ? "ja" : language.SelectedIndex == 2 ? "zh-CN" : language.SelectedIndex == 3 ? "ko" : "en";
        private void OpenSupportLink(object sender, EventArgs e) { string error; if (TryOpenSupportLink(Process.Start, out error)) { supportStatus.ForeColor = Color.FromArgb(17, 108, 75); supportStatus.Text = T("BrowserOpened"); } else { supportStatus.ForeColor = Color.FromArgb(176, 65, 56); supportStatus.Text = T("LinkFailed", error); } }
        private void TabsMouseMove(object sender, MouseEventArgs e) { var next = -1; for (var i = 0; i < tabs.TabCount; i++) if (tabs.GetTabRect(i).Contains(e.Location)) { next = i; break; } if (next == hoveredTabIndex) return; hoveredTabIndex = next; tabs.Invalidate(); }
        private void DrawTab(object sender, DrawItemEventArgs e)
        {
            var selected = e.Index == tabs.SelectedIndex; var hovered = e.Index == hoveredTabIndex; var support = tabs.TabPages[e.Index] == supportTab;
            var background = selected ? Color.White : hovered ? Color.FromArgb(247, 248, 250) : Color.FromArgb(240, 242, 245); var foreground = Color.FromArgb(42, 53, 69);
            if (support) { background = selected ? Color.FromArgb(255, 245, 226) : hovered ? Color.FromArgb(255, 238, 207) : Color.FromArgb(250, 231, 197); foreground = selected ? Color.FromArgb(137, 62, 7) : Color.FromArgb(174, 84, 13); }
            using (var brush = new SolidBrush(background)) e.Graphics.FillRectangle(brush, e.Bounds); TextRenderer.DrawText(e.Graphics, tabs.TabPages[e.Index].Text, tabs.Font, e.Bounds, foreground, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
            if (support && selected) using (var pen = new Pen(Color.FromArgb(213, 137, 32), 2f)) e.Graphics.DrawLine(pen, e.Bounds.Left + 4, e.Bounds.Bottom - 2, e.Bounds.Right - 4, e.Bounds.Bottom - 2);
        }

        protected override void OnResize(EventArgs e) { base.OnResize(e); PositionVersionLabel(); }
        private void PositionVersionLabel() { versionLabel.Location = new Point(Math.Max(8, ClientSize.Width - versionLabel.PreferredWidth - 12), Math.Max(8, ClientSize.Height - versionLabel.PreferredHeight - 8)); versionLabel.BringToFront(); }
        private static void Wide(TableLayoutPanel p, Control c, int row) { p.Controls.Add(c, 0, row); p.SetColumnSpan(c, 2); }
        private static Control SliderPanel(TrackBar s, Label v) { var p = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = Padding.Empty }; v.Margin = new Padding(8, 8, 0, 0); p.Controls.Add(s); p.Controls.Add(v); return p; }
        private static void ApplyFontFamily(Control root, string family) { root.Font = new Font(family, root.Font.Size, root.Font.Style); foreach (Control child in root.Controls) ApplyFontFamily(child, family); }
        private static CheckBox C() => new CheckBox { AutoSize = true, ForeColor = Color.FromArgb(48, 61, 80), Anchor = AnchorStyles.Left };
        private static Label L() => new Label { AutoSize = true, ForeColor = Color.FromArgb(45, 58, 77), Anchor = AnchorStyles.Left };
        private static NumericUpDown N(decimal min, decimal max, decimal inc) => new NumericUpDown { Minimum = min, Maximum = max, Increment = inc, Width = 120, ThousandsSeparator = true, Anchor = AnchorStyles.Left };
        private static decimal Bound(NumericUpDown c, decimal v) => Math.Max(c.Minimum, Math.Min(c.Maximum, v));
        private static int LanguageIndex(string value) { switch (Localization.NormalizeLanguage(value)) { case "ja": return 1; case "zh-CN": return 2; case "ko": return 3; default: return 0; } }
        private static void Primary(Button b) { b.FlatStyle = FlatStyle.Flat; b.BackColor = Color.FromArgb(202, 101, 20); b.ForeColor = Color.White; b.FlatAppearance.BorderColor = Color.FromArgb(164, 79, 12); b.Cursor = Cursors.Hand; }
        private static void Secondary(Button b) { b.FlatStyle = FlatStyle.Flat; b.BackColor = Color.FromArgb(244, 247, 251); b.ForeColor = Color.FromArgb(48, 61, 80); b.FlatAppearance.BorderColor = Color.FromArgb(171, 181, 196); b.Cursor = Cursors.Hand; }
        public static bool TryOpenSupportLink(Func<ProcessStartInfo, Process> opener, out string error) { try { opener(new ProcessStartInfo(SupportUrl) { UseShellExecute = true }); error = null; return true; } catch (Exception ex) { error = ex.Message; return false; } }
    }
}
