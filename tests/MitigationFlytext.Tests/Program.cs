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
            DecodeDamage(); TrackMitigation(); TrackEnemyDebuff(); RemoveAndExpire(); LocalizationAndCompatibility(); CatalogSanity(); UpdateSafety(); RenderPreview();
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
    private static void CatalogSanity() { Assert(MitigationCatalog.All.Count() >= 25, "catalog coverage"); Assert(MitigationCatalog.All.All(x => x.Percent > 0 && x.Percent <= 100), "catalog rates"); }
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
    private static string Damage(string at, string source, string name, string target, string amount) => "21|" + at + "|" + source + "|Boss|1234|" + name + "|" + target + "|Player|750003|" + amount + "|0|0|0|0|0|0|0|0|0|0|0|0|0|0|";
    private static void Assert(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
