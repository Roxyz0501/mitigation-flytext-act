using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace MitigationFlytext
{
    internal static class StatusIconStore
    {
        private static readonly object Gate = new object();
        private static readonly Dictionary<uint, Image> Cache = new Dictionary<uint, Image>();

        public static Image Get(uint statusId)
        {
            lock (Gate)
            {
                Image image;
                if (Cache.TryGetValue(statusId, out image)) return image;
                var resource = "MitigationFlytext.Assets.Status." + statusId + ".png";
                try
                {
                    using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
                    {
                        if (stream == null) { Cache[statusId] = null; return null; }
                        using (var loaded = Image.FromStream(stream)) image = new Bitmap(loaded);
                    }
                }
                catch { image = null; }
                Cache[statusId] = image;
                return image;
            }
        }
    }
}
