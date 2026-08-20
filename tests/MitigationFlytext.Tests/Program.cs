using MitigationFlytext;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        try
        {
            DecodeDamage(); TrackMitigation(); TrackZeroDamage(); TrackBarrierAbsorption(); TrackEnemyDebuff(); RemoveAndExpire(); LocalizationAndCompatibility(); CatalogSanity(); UpdateSafety(); PreviewToggleDoesNotHideWindow(); RenderPreview(); RenderBarrierFlytext(); RenderSettingsTabs();
            Console.WriteLine("MitigationFlytext tests: PASS"); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine("MitigationFlytext tests: FAIL\n" + ex); return 1; }
    }
    private static void DecodeDamage()
    {
        long value; Assert(FfxivAmountDecoder.TryDecode("47280000", out value) && value == 18216, "normal amount decode");
        Assert(FfxivAmountDecoder.TryDecode("426B4001", out value) && value == 82538, "large amount decode");
        Assert(FfxivAmountDecoder.TryDecode("424E400F", out value) && value == 999999, "999999 decode");
    }
    private static void TrackMitigation()
    {
        var t = new CombatLogTracker(); DamageFlytextEvent received = null; t.DamageReceived += (s, e) => received = e;
        t.ProcessLine("02|2026-01-01T00:00:00+00:00|10000001|Player|");
        t.ProcessLine("26|2026-01-01T00:00:01+00:00|4A7|Rampart|20.00|10000001|Player|10000001|Player|00|");
        t.ProcessLine(Damage("2026-01-01T00:00:02+00:00", "40000001", "Tankbuster", "10000001", "27100000"));
        Assert(received != null && received.Damage == 10000, "damage event"); Assert(received.EstimatedBeforeMitigation == 12500, "pre-mitigation estimate");
        Assert(received.Mitigations.Count == 1 && received.Mitigations[0].IsMine, "mine attribution");
    }
    private static void TrackEnemyDebuff()
    {
        var t = new CombatLogTracker(); DamageFlytextEvent received = null; t.DamageReceived += (s, e) => received = e;
        t.ProcessLine("02|2026-01-01T00:00:00+00:00|10000001|Player|");
        t.ProcessLine("26|2026-01-01T00:00:01+00:00|4A9|Reprisal|10.00|10000001|Player|40000001|Boss|00|");
        t.ProcessLine(Damage("2026-01-01T00:00:02+00:00", "40000001", "Raidwide", "10000001", "23280000"));
        Assert(received != null && received.Mitigations.Single().Definition.Name == "Reprisal", "attacker debuff"); Assert(received.Mitigations.Single().IsMine, "self reprisal");
    }
    private static void TrackZeroDamage()
    {
        var t = new CombatLogTracker(); DamageFlytextEvent received = null; t.DamageReceived += (s, e) => received = e;
        t.ProcessLine("02|2026-01-01T00:00:00+00:00|10000001|Player|");
        t.ProcessLine(Damage("2026-01-01T00:00:02+00:00", "40000001", "Invulnerable Hit", "10000001", "1000"));
        Assert(received != null && received.Damage == 0 && received.EstimatedBeforeMitigation == 0, "zero damage must be displayed");
    }
    private static void TrackBarrierAbsorption()
    {
        var t = new CombatLogTracker(); DamageFlytextEvent received = null; t.DamageReceived += (s, e) => received = e;
        t.ProcessLine("02|2026-01-01T00:00:00+00:00|10000001|Player|");
        t.ProcessLine("26|2026-01-01T00:00:00.5+00:00|129|Galvanize|30.00|10000002|Scholar|10000001|Player|00|");
        t.ProcessLine("38|2026-01-01T00:00:01+00:00|10000001|Player|0|50000|50000|10000|10000|20|0|");
        t.ProcessLine(DamageWithSequence("2026-01-01T00:00:02+00:00", "40000001", "Barrier Hit", "10000001", "17700000", "00001234"));
        t.ProcessLine("37|2026-01-01T00:00:02.1+00:00|10000001|Player|00001234|49000|50000|10000|10000|10|0|");
        Assert(received != null && received.BarrierAbsorbed == 5000 && received.Damage == 1000, "barrier absorption must reduce actual HP damage");
        Assert(received.Mitigations.Any(x => x.Definition.HasBarrier && x.Definition.Name == "Galvanize"), "barrier skill attribution");
    }
    private static void RemoveAndExpire()
    {
        var t = new CombatLogTracker(); var events = new List<DamageFlytextEvent>(); t.DamageReceived += (s, e) => events.Add(e); t.ProcessLine("02|2026-01-01T00:00:00+00:00|10000001|Player|");
        t.ProcessLine("26|2026-01-01T00:00:01+00:00|4A7|Rampart|1.00|10000001|Player|10000001|Player|00|");
        t.ProcessLine("30|2026-01-01T00:00:01.5+00:00|4A7|Rampart|0|10000001|Player|10000001|Player|");
        t.ProcessLine(Damage("2026-01-01T00:00:02+00:00", "40000001", "Hit", "10000001", "03E80000")); Assert(events.Single().Mitigations.Count == 0, "status remove");
    }
    private static void LocalizationAndCompatibility()
    {
        Assert(Localization.MapCulture(new CultureInfo("ja-JP")) == "ja", "ja map"); Assert(Localization.MapCulture(new CultureInfo("zh-TW")) == "zh-CN", "zh map");
        Assert(Localization.MapCulture(new CultureInfo("ko-KR")) == "ko", "ko map"); Assert(Localization.Get("xx", "Support") == "Support", "fallback");
        var s = new PluginSettings(); Assert(s.InitializeLanguageIfMissing(new CultureInfo("ja-JP")) && s.Language == "ja", "first language"); Assert(!s.InitializeLanguageIfMissing(new CultureInfo("ko-KR")) && s.Language == "ja", "saved language");
    }
    private static void CatalogSanity() { Assert(MitigationCatalog.All.Count() >= 50, "catalog coverage"); Assert(MitigationCatalog.All.All(x => (x.Percent > 0 && x.Percent <= 100) || x.HasBarrier), "catalog rates/barriers"); Assert(MitigationCatalog.All.All(x => StatusIconStore.Get(x.StatusId) != null), "all mitigation icons must be embedded"); }
    private static void UpdateSafety()
    {
        Assert(UpdateConfiguration.IsConfigured && UpdateConfiguration.RepositoryName == "mitigation-flytext-act", "repository config");
        var json = "[{\"tag_name\":\"v1.1.0-beta.1\",\"draft\":false,\"prerelease\":true,\"assets\":[]},{\"tag_name\":\"v1.0.1\",\"body\":\"Fix\",\"draft\":false,\"prerelease\":false,\"assets\":[]}]";
        var result = UpdateService.EvaluateResponse(json, new Version(1, 0, 0)); Assert(result.Status == UpdateCheckStatus.UpdateAvailable && result.Release.Version.ToString() == "1.0.1", "stable release parse");
        var root = Path.Combine(Path.GetTempPath(), "MitigationFlytextTests", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try
        {
            var file = Path.Combine(root, "asset.zip"); File.WriteAllText(file, "known", Encoding.UTF8); var hash = UpdatePackageVerifier.ComputeSha256(file);
            Assert(UpdatePackageVerifier.VerifySha256(file, hash), "sha verify"); Assert(UpdatePackageVerifier.FindManifestHash(hash + "  asset.zip", "asset.zip") == hash, "manifest");
            var bad = Path.Combine(root, "bad.zip"); using (var z = ZipFile.Open(bad, ZipArchiveMode.Create)) z.CreateEntry("../escape.dll");
            bool rejected = false; try { UpdatePackageVerifier.ExtractValidated(bad, Path.Combine(root, "out")); } catch (InvalidDataException) { rejected = true; } Assert(rejected, "zip slip");
        }
        finally { Directory.Delete(root, true); }
    }
    private static void RenderPreview()
    {
        var settings = new PluginSettings { Language = "ja", Preview = true, Locked = false, Left = 0, Top = 0, Width = 680, Height = 220, FontSize = 20 };
        using (var form = new OverlayForm(settings)) using (var bitmap = new Bitmap(settings.Width, settings.Height))
        {
            form.ApplySettings(settings, true); form.Bounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height); form.DrawToBitmap(bitmap, form.ClientRectangle);
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MitigationFlytext-preview.png"); bitmap.Save(path); Assert(new FileInfo(path).Length > 1000, "preview render");
        }
    }
    private static void PreviewToggleDoesNotHideWindow()
    {
        var settings = new PluginSettings { Preview = true, OverlayEnabled = true, Locked = false, Width = 480, Height = 180 };
        using (var form = new OverlayForm(settings))
        {
            form.ApplySettings(settings, true);
            Assert(form.Visible && form.ContentVisibleForTest && form.Opacity > 0, "preview should be visible");
            settings.Preview = false; form.ApplySettings(settings);
            Assert(form.Visible && !form.ContentVisibleForTest && form.Opacity == 0, "preview off should retain a transparent window instead of Hide/Show flicker");
            settings.Preview = true; form.ApplySettings(settings);
            Assert(form.Visible && form.ContentVisibleForTest && form.Opacity > 0, "preview should return without recreating the window");
        }
    }
    private static void RenderBarrierFlytext()
    {
        MitigationDefinition barrier; MitigationDefinition reduction; Assert(MitigationCatalog.TryGet(297, out barrier), "barrier preview catalog entry"); Assert(MitigationCatalog.TryGet(1193, out reduction), "reduction preview catalog entry");
        var settings = new PluginSettings { Language = "ja", Preview = false, OverlayEnabled = true, Locked = false, Left = 0, Top = 0, Width = 760, Height = 240, FontSize = 20 };
        using (var form = new OverlayForm(settings)) using (var bitmap = new Bitmap(settings.Width, settings.Height))
        {
            form.ApplySettings(settings, true); form.Bounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            form.Push(new DamageFlytextEvent { TimestampUtc = DateTime.UtcNow, ActionName = "レイドワイド", Damage = 18000, EstimatedBeforeMitigation = 30000, TotalMitigationPercent = 20, BarrierAbsorbed = 6000, Mitigations = new List<ActiveMitigation> { new ActiveMitigation { Definition = barrier, IsMine = true }, new ActiveMitigation { Definition = reduction } } });
            form.DrawToBitmap(bitmap, form.ClientRectangle); var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MitigationFlytext-barrier.png"); bitmap.Save(path); Assert(new FileInfo(path).Length > 1000, "barrier flytext render");
        }
    }
    private static void RenderSettingsTabs()
    {
        var output = AppDomain.CurrentDomain.BaseDirectory;
        using (var control = new SettingsControl(new PluginSettings { Language = "ja" }))
        {
            control.Size = new Size(780, 460); var tab = Descendants(control).OfType<TabControl>().Single(); Assert(tab.TabCount == 3, "settings/update/support tabs");
            for (var i = 0; i < tab.TabCount; i++)
            {
                tab.SelectedIndex = i; control.PerformLayout(); using (var bitmap = new Bitmap(control.Width, control.Height)) { control.DrawToBitmap(bitmap, control.ClientRectangle); bitmap.Save(Path.Combine(output, "MitigationFlytext-settings-" + i + ".png")); }
            }
            Assert(Descendants(tab.TabPages[0]).OfType<CheckBox>().All(x => x.Height >= 17), "checkboxes must not collapse");
        }
    }
    private static IEnumerable<Control> Descendants(Control root) { foreach (Control child in root.Controls) { yield return child; foreach (var nested in Descendants(child)) yield return nested; } }
    private static string Damage(string at, string source, string name, string target, string amount) => "21|" + at + "|" + source + "|Boss|1234|" + name + "|" + target + "|Player|750003|" + amount + "|0|0|0|0|0|0|0|0|0|0|0|0|0|0|";
    private static string DamageWithSequence(string at, string source, string name, string target, string amount, string sequence)
    {
        var fields = new string[47]; fields[0] = "21"; fields[1] = at; fields[2] = source; fields[3] = "Boss"; fields[4] = "1234"; fields[5] = name; fields[6] = target; fields[7] = "Player"; fields[8] = "750003"; fields[9] = amount;
        for (var i = 10; i < fields.Length; i++) fields[i] = "0"; fields[44] = sequence; fields[45] = "0"; fields[46] = "1"; return string.Join("|", fields) + "|";
    }
    private static void Assert(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
