using Advanced_Combat_Tracker;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace MitigationFlytext
{
    public sealed class MitigationFlytextPlugin : IActPluginV1
    {
        private readonly CombatLogTracker tracker = new CombatLogTracker();
        private readonly UpdateService updateService = new UpdateService();
        private readonly CancellationTokenSource updateCancellation = new CancellationTokenSource();
        private PluginSettings settings; private SettingsControl control; private OverlayForm overlay; private Label status; private TabPage page; private string settingsPath;
        public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
        {
            status = pluginStatusText; page = pluginScreenSpace; settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Advanced Combat Tracker", "Config", "MitigationFlytext.xml");
            settings = PluginSettings.Load(settingsPath); if (settings.InitializeLanguageIfMissing(CultureInfo.CurrentUICulture)) Save(); settings.Normalize();
            control = new SettingsControl(settings); control.SettingsChanged += SettingsChanged; control.CheckUpdatesRequested += delegate { CheckForUpdates(); }; control.InstallUpdateRequested += InstallUpdate; control.LaterRequested += Later; page.Controls.Add(control); page.Text = Localization.Get(settings.Language, "PluginTab");
            overlay = new OverlayForm(settings); overlay.BoundsChangedByUser += delegate { Save(); }; overlay.ApplySettings(settings, true);
            ReplayCurrentLog(); tracker.DamageReceived += DamageReceived; ActGlobals.oFormActMain.OnLogLineRead += LogLineRead; status.Text = Localization.Get(settings.Language, "Started"); if (settings.CheckUpdatesOnStartup) CheckForUpdates();
        }
        private void LogLineRead(bool isImport, LogLineEventArgs info) { if (isImport || info == null) return; tracker.ProcessLine(info.originalLogLine); if (!string.Equals(info.originalLogLine, info.logLine, StringComparison.Ordinal)) tracker.ProcessLine(info.logLine); }
        private void DamageReceived(object sender, DamageFlytextEvent e) { overlay?.Push(e); }
        private void SettingsChanged(object sender, EventArgs e) { control.ApplyTo(settings); overlay.ApplySettings(settings); page.Text = Localization.Get(settings.Language, "PluginTab"); status.Text = Localization.Get(settings.Language, "Started"); Save(); }
        private void Save() { try { settings?.Save(settingsPath); } catch (Exception ex) { if (status != null) status.Text = Localization.Get(settings == null ? "en" : settings.Language, "SaveError", ex.Message); } }
        private void ReplayCurrentLog()
        {
            try
            {
                var path = ActGlobals.oFormActMain.LogFilePath; if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return;
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    var start = Math.Max(0, stream.Length - 32L * 1024 * 1024); stream.Seek(start, SeekOrigin.Begin);
                    using (var reader = new StreamReader(stream)) { if (start > 0) reader.ReadLine(); string line; while ((line = reader.ReadLine()) != null) tracker.ProcessLine(line); }
                }
            }
            catch { }
        }
        private async void CheckForUpdates()
        {
            if (control == null || control.IsDisposed) return; control.SetUpdateChecking(); var current = Assembly.GetExecutingAssembly().GetName().Version;
            var result = await updateService.CheckAsync(current, updateCancellation.Token); if (control == null || control.IsDisposed || updateCancellation.IsCancellationRequested) return; control.ShowUpdateResult(result, current);
        }
        private async void InstallUpdate(object sender, EventArgs e)
        {
            var release = control == null ? null : control.AvailableRelease; if (release == null) return; control.SetUpdateText("Preparing");
            try { var prepared = await updateService.DownloadAndVerifyAsync(release, updateCancellation.Token); if (updateCancellation.IsCancellationRequested) return; updateService.LaunchUpdater(prepared, Assembly.GetExecutingAssembly().Location); control.SetUpdateText("Prepared"); }
            catch (Exception ex) { control.SetUpdateText("UpdateFailed", ex.GetBaseException().Message); }
        }
        private void Later(object sender, EventArgs e) { if (control.AvailableRelease != null) { settings.SkippedVersion = control.AvailableRelease.Version.ToString(); Save(); } control.SetUpdateText("Later"); }
        public void DeInitPlugin()
        {
            ActGlobals.oFormActMain.OnLogLineRead -= LogLineRead; tracker.DamageReceived -= DamageReceived; updateCancellation.Cancel(); if (control != null) control.SettingsChanged -= SettingsChanged; Save();
            overlay?.Close(); overlay?.Dispose(); control?.Dispose(); updateService.Dispose(); updateCancellation.Dispose(); if (status != null) status.Text = Localization.Get(settings == null ? "en" : settings.Language, "Stopped");
        }
    }
}
