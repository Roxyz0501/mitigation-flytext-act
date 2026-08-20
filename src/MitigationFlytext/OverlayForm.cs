using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace MitigationFlytext
{
    public sealed class OverlayForm : Form
    {
        private const int WS_EX_TOOLWINDOW = 0x80, WS_EX_NOACTIVATE = 0x08000000, WM_NCHITTEST = 0x84, HTTRANSPARENT = -1, HTCLIENT = 1, HTCAPTION = 2;
        private readonly List<DamageFlytextEvent> events = new List<DamageFlytextEvent>();
        private readonly Timer animation = new Timer { Interval = 33 };
        private readonly Color key = Color.FromArgb(1, 2, 3);
        private PluginSettings settings;
        private bool contentVisible;
        public event EventHandler BoundsChangedByUser;
        public OverlayForm(PluginSettings value)
        {
            settings = value; AutoScaleMode = AutoScaleMode.None; BackColor = key; TransparencyKey = key;
            FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; StartPosition = FormStartPosition.Manual; TopMost = true; DoubleBuffered = true;
            MinimumSize = new Size(360, 160); MaximumSize = new Size(1400, 900); SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            animation.Tick += delegate { Expire(); if (events.Count > 0) Invalidate(); }; animation.Start(); Move += BoundsChanged; ResizeEnd += BoundsChanged;
        }
        protected override bool ShowWithoutActivation => true;
        protected override CreateParams CreateParams { get { var p = base.CreateParams; p.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE; return p; } }
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST)
            {
                if (!contentVisible || settings.Locked) { m.Result = (IntPtr)HTTRANSPARENT; return; }
                base.WndProc(ref m); if ((int)m.Result == HTCLIENT) m.Result = (IntPtr)HTCAPTION; return;
            }
            base.WndProc(ref m);
        }
        public void ApplySettings(PluginSettings value, bool restore = false)
        {
            settings = value; settings.Normalize();
            if (restore) { var r = new Rectangle(settings.Left, settings.Top, settings.Width, settings.Height); if (!Screen.AllScreens.Any(x => x.WorkingArea.IntersectsWith(r))) r.Location = new Point(100, 100); Bounds = r; }
            RefreshVisible(); Invalidate();
        }
        public void Push(DamageFlytextEvent value)
        {
            if (InvokeRequired) { BeginInvoke(new Action<DamageFlytextEvent>(Push), value); return; }
            events.Insert(0, value); while (events.Count > settings.MaximumLines) events.RemoveAt(events.Count - 1); RefreshVisible(); Invalidate();
        }
        private void Expire() { var limit = DateTime.UtcNow.AddMilliseconds(-settings.DurationMilliseconds); events.RemoveAll(x => x.TimestampUtc < limit); RefreshVisible(); }
        private void RefreshVisible()
        {
            var show = settings.OverlayEnabled && (settings.Preview || events.Count > 0);
            contentVisible = show;
            var targetOpacity = show ? settings.OpacityPercent / 100d : 0d;
            if (!Visible) { Opacity = 0; Show(); }
            if (Math.Abs(Opacity - targetOpacity) > .001d) Opacity = targetOpacity;
        }
        internal bool ContentVisibleForTest => contentVisible;
        private void BoundsChanged(object sender, EventArgs e) { if (settings.Locked) return; settings.Left = Left; settings.Top = Top; settings.Width = Width; settings.Height = Height; BoundsChangedByUser?.Invoke(this, EventArgs.Empty); }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            if (settings.Preview && events.Count == 0) { DrawPreview(e.Graphics); return; }
            var y = Height - 12f; var now = DateTime.UtcNow;
            foreach (var item in events.Take(settings.MaximumLines))
            {
                var age = (now - item.TimestampUtc).TotalMilliseconds; var progress = Math.Max(0, Math.Min(1, age / settings.DurationMilliseconds));
                var alpha = progress < .68 ? 255 : (int)(255 * (1 - progress) / .32); var rise = (float)(progress * 44);
                y -= DrawItem(e.Graphics, item, y - rise, alpha) + 10;
            }
        }
        private float DrawItem(Graphics g, DamageFlytextEvent item, float bottom, int alpha)
        {
            using (var titleFont = new Font(Localization.FontFamily(settings.Language), settings.FontSize, FontStyle.Bold))
            using (var detailFont = new Font(Localization.FontFamily(settings.Language), Math.Max(9, settings.FontSize * .55f), FontStyle.Regular))
            using (var shadow = new SolidBrush(Color.FromArgb(alpha * 3 / 4, 0, 0, 0)))
            using (var primary = new SolidBrush(Color.FromArgb(alpha, 255, 236, 225)))
            using (var accent = new SolidBrush(Color.FromArgb(alpha, 255, 114, 92)))
            {
                var first = item.ActionName + "  " + item.Damage.ToString("N0", CultureInfo.CurrentCulture);
                var second = " [" + item.EstimatedBeforeMitigation.ToString("N0", CultureInfo.CurrentCulture) + "]  (-" + item.TotalMitigationPercent.ToString("0.#", CultureInfo.CurrentCulture) + "%)";
                var icons = item.Mitigations ?? new List<ActiveMitigation>(); var barriers = icons.Where(mitigation => mitigation.Definition.HasBarrier).ToList();
                var barrierText = item.BarrierAbsorbed > 0 && barriers.Count > 0 ? Localization.Get(settings.Language, "BarrierAbsorbed", item.BarrierAbsorbed.ToString("N0", CultureInfo.CurrentCulture), string.Join(" / ", barriers.Select(mitigation => mitigation.Definition.Name))) : null;
                var a = g.MeasureString(first, titleFont); var b = g.MeasureString(second, detailFont); var titleHeight = Math.Max(a.Height, b.Height); var barrierHeight = barrierText == null ? 0 : g.MeasureString(barrierText, detailFont).Height + 3;
                var iconSize = Math.Max(22, settings.FontSize + 5); var gap = 5; var iconHeight = icons.Count > 0 ? iconSize + 5 : 0; var totalHeight = titleHeight + barrierHeight + iconHeight; var top = bottom - totalHeight;
                var total = a.Width + b.Width; var x = Math.Max(8, (Width - total) / 2);
                g.DrawString(first, titleFont, shadow, x + 2, top + 2); g.DrawString(first, titleFont, primary, x, top);
                g.DrawString(second, detailFont, shadow, x + a.Width + 2, top + titleHeight - b.Height + 1); g.DrawString(second, detailFont, accent, x + a.Width, top + titleHeight - b.Height - 1);
                var nextY = top + titleHeight;
                if (barrierText != null)
                {
                    var size = g.MeasureString(barrierText, detailFont); var bx = Math.Max(8, (Width - size.Width) / 2);
                    using (var barrierBrush = new SolidBrush(Color.FromArgb(alpha, 104, 224, 255))) g.DrawString(barrierText, detailFont, barrierBrush, bx, nextY);
                    nextY += barrierHeight;
                }
                var iconsWidth = icons.Count * (iconSize + gap); var ix = Math.Max(8, (Width - iconsWidth) / 2); var iy = nextY + 3;
                foreach (var mitigation in icons) { DrawIcon(g, mitigation, new RectangleF(ix, iy, iconSize, iconSize), alpha); ix += iconSize + gap; }
                return totalHeight;
            }
        }
        private static void DrawIcon(Graphics g, ActiveMitigation m, RectangleF r, int alpha)
        {
            if (m.IsMine) using (var glow = new SolidBrush(Color.FromArgb(alpha / 3, 255, 190, 38))) g.FillEllipse(glow, r.X - 5, r.Y - 5, r.Width + 10, r.Height + 10);
            var icon = StatusIconStore.Get(m.Definition.StatusId);
            using (var path = Rounded(r, 5))
            using (var border = new Pen(m.IsMine ? Color.FromArgb(alpha, 255, 192, 48) : Color.FromArgb(alpha, 170, 190, 216), m.IsMine ? 3f : 1.5f))
            {
                if (icon != null) g.DrawImage(icon, r);
                else
                {
                    using (var fill = new LinearGradientBrush(r, Color.FromArgb(alpha, 66, 87, 121), Color.FromArgb(alpha, 24, 34, 54), 90)) g.FillPath(fill, path);
                    using (var f = new Font("Segoe UI", Math.Max(7, r.Width * .28f), FontStyle.Bold))
                    using (var b = new SolidBrush(Color.FromArgb(alpha, 255, 255, 255)))
                    { var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center }; g.DrawString(m.Definition.Abbreviation, f, b, r, format); }
                }
                g.DrawPath(border, path);
            }
        }
        private void DrawPreview(Graphics g)
        {
            using (var b = new SolidBrush(Color.FromArgb(210, 22, 30, 44))) g.FillRectangle(b, ClientRectangle);
            using (var p = new Pen(Color.FromArgb(220, 225, 153, 42), 2)) g.DrawRectangle(p, 1, 1, Width - 3, Height - 3);
            using (var f = new Font(Localization.FontFamily(settings.Language), settings.FontSize, FontStyle.Bold)) using (var b = new SolidBrush(Color.White))
                g.DrawString(Localization.Get(settings.Language, "PreviewText"), f, b, new PointF(16, 18));
            using (var f = new Font(Localization.FontFamily(settings.Language), Math.Max(9, settings.FontSize * .55f))) using (var b = new SolidBrush(Color.FromArgb(230, 230, 235, 242)))
                g.DrawString(Localization.Get(settings.Language, "PreviewDetail"), f, b, new PointF(18, 62));
            var def1 = new MitigationDefinition(1193, "Reprisal", 10, MitigationScope.OnAttacker, "RP"); var def2 = new MitigationDefinition(2618, "Kerachole", 10, MitigationScope.OnPlayer, "KE");
            DrawIcon(g, new ActiveMitigation { Definition = def1, IsMine = true }, new RectangleF(20, 96, 34, 34), 255); DrawIcon(g, new ActiveMitigation { Definition = def2 }, new RectangleF(62, 96, 34, 34), 255);
        }
        private static GraphicsPath Rounded(RectangleF r, float radius) { var p = new GraphicsPath(); var d = radius * 2; p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90); p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90); p.CloseFigure(); return p; }
        protected override void Dispose(bool disposing) { if (disposing) animation.Dispose(); base.Dispose(disposing); }
    }
}
