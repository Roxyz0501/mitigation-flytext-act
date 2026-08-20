using System;
using System.Globalization;

namespace MitigationFlytext
{
    public static class FfxivAmountDecoder
    {
        public static bool TryDecode(string text, out long amount)
        {
            amount = 0;
            uint raw;
            if (string.IsNullOrWhiteSpace(text) || !uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out raw)) return false;
            var hex = raw.ToString("X8", CultureInfo.InvariantCulture);
            var a = Convert.ToInt32(hex.Substring(0, 2), 16);
            var b = Convert.ToInt32(hex.Substring(2, 2), 16);
            var c = Convert.ToInt32(hex.Substring(4, 2), 16);
            var d = Convert.ToInt32(hex.Substring(6, 2), 16);
            if ((c & 0x40) != 0)
                amount = (d << 16) | (a << 8) | ((b - d) & 0xFF);
            else
                amount = (a << 8) | b;
            return true;
        }
    }
}
