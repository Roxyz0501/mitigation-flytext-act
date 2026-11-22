using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Roxyz.Act.UI;

namespace MitigationFlytext
{
    public sealed class SettingsControl : UserControl
    {
        public const string SupportUrl = "https://ko-fi.com/roxyz0501";
        private readonly PluginSettings settings;
        private readonly Label headerTitle = L(), headerSubtitle = L();
        private readonly PluginTabs tabs;
        private readonly TabPage settingsTab = new TabPage(), supportTab = new TabPage();
        private readonly PluginUpdatePage updateTab;
        private readonly ComboBox language = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        private readonly CheckBox enabled = C(), locked = C(), preview = C();
        private readonly NumericUpDown duration = N(1200, 10000, 100), lines = N(1, 12, 1), fontSize = N(12, 40, 1);
        private readonly TrackBar opacity = new TrackBar { Minimum = 30, Maximum = 100, TickFrequency = 5, AutoSize = false, Height = 36, Width = 190 };
        private readonly Label languageLabel = L(), durationLabel = L(), linesLabel = L(), opacityLabel = L(), opacityValue = L(), fontSizeLabel = L();
        private readonly Label hint = L(), estimate = L(), versionLabel = L();
        private readonly Label supportHeading = L(), supportDescription = L(), supportOptional = L(), supportUrl = L(), supportSafety = L(), supportStatus = L();
        private readonly Button supportButton = new Button();
        private bool loading = true;
        private string updateMessageKey = "NotChecked";
        private object[] updateMessageArgs = new object[0];
        private UpdatePageState updatePageState = UpdatePageState.Ready;
        private string updateReleaseNotes;

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
            fontSize.Value = Bound(fontSize, settings.FontSize);

            var header = BuildHeader();
            updateTab = new PluginUpdatePage("mitigation-flytext-act", Assembly.GetExecutingAssembly().GetName().Version.ToString(3), settings.CheckUpdatesOnStartup, settings.Language);
            tabs = new PluginTabs(supportTab);
            settingsTab.BackColor = Color.White; settingsTab.Padding = new Padding(4);
            supportTab.BackColor = Color.White; supportTab.Padding = new Padding(4);
            settingsTab.Controls.Add(BuildSettingsLayout()); supportTab.Controls.Add(BuildSupportLayout());
            tabs.AddContentPage(settingsTab); tabs.AddContentPage(updateTab); tabs.AddContentPage(supportTab);
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

        private Control BuildSettingsLayout()
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, AutoScroll = true };
            var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, RowCount = 9, Padding = new Padding(24, 20, 24, 20), BackColor = Color.White };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 235)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            var heights = new[] { 42, 34, 34, 34, 42, 42, 46, 42, 62 }; foreach (var h in heights) layout.RowStyles.Add(new RowStyle(SizeType.Absolute, h));
            layout.Controls.Add(languageLabel, 0, 0); layout.Controls.Add(language, 1, 0);
            Wide(layout, enabled, 1); Wide(layout, locked, 2); Wide(layout, preview, 3);
            layout.Controls.Add(durationLabel, 0, 4); layout.Controls.Add(duration, 1, 4); layout.Controls.Add(linesLabel, 0, 5); layout.Controls.Add(lines, 1, 5);
            layout.Controls.Add(opacityLabel, 0, 6); layout.Controls.Add(SliderPanel(opacity, opacityValue), 1, 6); layout.Controls.Add(fontSizeLabel, 0, 7); layout.Controls.Add(fontSize, 1, 7);
            hint.AutoSize = false; hint.Dock = DockStyle.Fill; hint.ForeColor = Color.FromArgb(73, 96, 128); hint.Padding = new Padding(0, 8, 0, 0); Wide(layout, hint, 8);
            panel.Controls.Add(layout); return panel;
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
            opacity.ValueChanged += delegate { opacityValue.Text = opacity.Value + "%"; Changed(this, EventArgs.Empty); }; fontSize.ValueChanged += Changed;
            updateTab.StartupChanged += Changed;
            updateTab.CheckRequested += delegate { CheckUpdatesRequested?.Invoke(this, EventArgs.Empty); };
            updateTab.InstallRequested += delegate { InstallUpdateRequested?.Invoke(this, EventArgs.Empty); };
            updateTab.LaterRequested += delegate { LaterRequested?.Invoke(this, EventArgs.Empty); };
            supportButton.Click += OpenSupportLink;
        }

        public void ApplyTo(PluginSettings value)
        {
            value.Language = CurrentLanguage; value.OverlayEnabled = enabled.Checked; value.Locked = locked.Checked; value.Preview = preview.Checked; value.DurationMilliseconds = (int)duration.Value;
            value.MaximumLines = (int)lines.Value; value.OpacityPercent = opacity.Value; value.FontSize = (int)fontSize.Value; value.CheckUpdatesOnStartup = updateTab.CheckOnStartup; value.Normalize();
        }

        internal void SetUpdateChecking() { AvailableRelease = null; SetUpdateState(UpdatePageState.Checking, "Checking"); }
        internal void ShowUpdateResult(UpdateCheckResult result, Version current)
        {
            AvailableRelease = result == null ? null : result.Release;
            if (result == null || result.Status == UpdateCheckStatus.Failed || result.Status == UpdateCheckStatus.RepositoryNotConfigured) SetUpdateState(UpdatePageState.Ready, "CheckFailed");
            else if (result.Status == UpdateCheckStatus.NoStableRelease) SetUpdateState(UpdatePageState.Ready, "NoRelease");
            else if (result.Status == UpdateCheckStatus.UpToDate) SetUpdateState(UpdatePageState.Ready, "UpToDate", current.ToString(3));
            else
            {
                updateReleaseNotes = string.IsNullOrWhiteSpace(result.Release.Notes) ? null : result.Release.Notes;
                SetUpdateState(UpdatePageState.Available, "UpdateAvailable", current.ToString(3), result.Release.Version);
            }
        }
        internal void SetUpdateText(string key, string detail = null)
        {
            var state = key == "Preparing" ? UpdatePageState.Preparing : key == "Prepared" ? UpdatePageState.Prepared : key == "UpdateFailed" ? UpdatePageState.PrepareFailed : UpdatePageState.Ready;
            SetUpdateState(state, key, detail == null ? new object[0] : new object[] { detail });
        }
        internal void ShowUpdateSkipped(string version) { SetUpdateState(UpdatePageState.Ready, "UpdateSkipped", version); }
        internal void FocusUpdateTab() { tabs.SelectedTab = updateTab; }

        private void SetUpdateState(UpdatePageState state, string key, params object[] args)
        {
            updatePageState = state; updateMessageKey = key; updateMessageArgs = args ?? new object[0];
            if (state != UpdatePageState.Available) updateReleaseNotes = null;
            RenderUpdatePage();
        }

        private void RenderUpdatePage()
        {
            var message = T(updateMessageKey, updateMessageArgs);
            if (updatePageState == UpdatePageState.Available)
                message += "\r\n\r\n" + T("ReleaseNotes") + ":\r\n" + (string.IsNullOrWhiteSpace(updateReleaseNotes) ? T("NoReleaseNotes") : updateReleaseNotes);
            updateTab.ShowState(updatePageState, message);
        }

        private void Changed(object sender, EventArgs e) { if (loading) return; ApplyTo(settings); ApplyLanguage(); SettingsChanged?.Invoke(this, EventArgs.Empty); }
        private void ApplyLanguage()
        {
            var family = Localization.FontFamily(CurrentLanguage); ApplyFontFamily(this, family);
            headerTitle.Text = T("PluginTab"); headerTitle.Font = new Font(family, 17f, FontStyle.Bold); headerSubtitle.Text = T("Subtitle"); headerSubtitle.Font = new Font(family, 8.5f);
            tabs.SetLanguageFont(family); settingsTab.Text = T("Settings"); supportTab.Text = T("Support"); updateTab.SetLanguage(CurrentLanguage);
            languageLabel.Text = T("Language"); enabled.Text = T("Enabled"); locked.Text = T("Locked"); preview.Text = T("Preview"); durationLabel.Text = T("Duration"); linesLabel.Text = T("Lines"); opacityLabel.Text = T("Opacity"); fontSizeLabel.Text = T("FontSize"); hint.Text = T("Hint"); estimate.Text = T("EstimateNote");
            supportHeading.Text = T("SupportHeading"); supportHeading.Font = new Font(family, 18f, FontStyle.Bold); supportDescription.Text = T("SupportDescription"); supportOptional.Text = T("SupportBody"); supportOptional.Font = new Font(family, 9.5f, FontStyle.Bold); supportButton.Text = T("SupportButton"); supportSafety.Text = T("SupportSafety");
            RenderUpdatePage(); Invalidate(true);
        }

        private string T(string key, params object[] args) => Localization.Get(CurrentLanguage, key, args);
        private string CurrentLanguage => language.SelectedIndex == 1 ? "ja" : language.SelectedIndex == 2 ? "zh-CN" : language.SelectedIndex == 3 ? "ko" : "en";
        private void OpenSupportLink(object sender, EventArgs e) { string error; if (TryOpenSupportLink(Process.Start, out error)) { supportStatus.ForeColor = Color.FromArgb(17, 108, 75); supportStatus.Text = T("BrowserOpened"); } else { supportStatus.ForeColor = Color.FromArgb(176, 65, 56); supportStatus.Text = T("LinkFailed", error); } }

        protected override void OnResize(EventArgs e) { base.OnResize(e); PositionVersionLabel(); }
        private void PositionVersionLabel() { versionLabel.Location = new Point(Math.Max(8, ClientSize.Width - versionLabel.PreferredWidth - 12), Math.Max(8, ClientSize.Height - versionLabel.PreferredHeight - 8)); versionLabel.BringToFront(); }
        private static void Wide(TableLayoutPanel p, Control c, int row) { p.Controls.Add(c, 0, row); p.SetColumnSpan(c, 2); }
        private static Control SliderPanel(TrackBar s, Label v) { var p = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = Padding.Empty }; v.Margin = new Padding(8, 8, 0, 0); p.Controls.Add(s); p.Controls.Add(v); return p; }
        private static void ApplyFontFamily(Control root, string family) { if (root is PluginUpdatePage || root is PluginTabs) return; root.Font = new Font(family, root.Font.Size, root.Font.Style); foreach (Control child in root.Controls) ApplyFontFamily(child, family); }
        private static CheckBox C() => new CheckBox { AutoSize = true, ForeColor = Color.FromArgb(48, 61, 80), Anchor = AnchorStyles.Left };
        private static Label L() => new Label { AutoSize = true, ForeColor = Color.FromArgb(45, 58, 77), Anchor = AnchorStyles.Left };
        private static NumericUpDown N(decimal min, decimal max, decimal inc) => new NumericUpDown { Minimum = min, Maximum = max, Increment = inc, Width = 120, ThousandsSeparator = true, Anchor = AnchorStyles.Left };
        private static decimal Bound(NumericUpDown c, decimal v) => Math.Max(c.Minimum, Math.Min(c.Maximum, v));
        private static int LanguageIndex(string value) { switch (Localization.NormalizeLanguage(value)) { case "ja": return 1; case "zh-CN": return 2; case "ko": return 3; default: return 0; } }
        private static void Primary(Button b) { b.FlatStyle = FlatStyle.Flat; b.BackColor = Color.FromArgb(202, 101, 20); b.ForeColor = Color.White; b.FlatAppearance.BorderColor = Color.FromArgb(164, 79, 12); b.Cursor = Cursors.Hand; }
        public static bool TryOpenSupportLink(Func<ProcessStartInfo, Process> opener, out string error) { try { opener(new ProcessStartInfo(SupportUrl) { UseShellExecute = true }); error = null; return true; } catch (Exception ex) { error = ex.Message; return false; } }
    }
}
