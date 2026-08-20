using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MitigationFlytext
{
    public sealed class CombatLogTracker
    {
        private readonly object gate = new object();
        private readonly Dictionary<string, ActiveMitigation> active = new Dictionary<string, ActiveMitigation>();
        public uint PlayerId { get; private set; }
        public event EventHandler<DamageFlytextEvent> DamageReceived;

        public bool ProcessLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line) || line.IndexOf('|') < 0) return false;
            var f = line.Split('|');
            if (f.Length < 3) return false;
            if (f[0] == "02") return ParsePlayer(f);
            if (f[0] == "26") return ParseStatusAdd(f);
            if (f[0] == "30") return ParseStatusRemove(f);
            if (f[0] == "21" || f[0] == "22") return ParseDamage(f);
            return false;
        }

        private bool ParsePlayer(string[] f)
        {
            uint id;
            if (f.Length < 4 || !Hex(f[2], out id)) return false;
            lock (gate) { PlayerId = id; active.Clear(); }
            return true;
        }

        private bool ParseStatusAdd(string[] f)
        {
            uint statusId, sourceId, targetId; double seconds;
            MitigationDefinition definition;
            if (f.Length < 9 || !Hex(f[2], out statusId) || !MitigationCatalog.TryGet(statusId, out definition) ||
                !Hex(f[5], out sourceId) || !Hex(f[7], out targetId) ||
                !double.TryParse(f[4], NumberStyles.Float, CultureInfo.InvariantCulture, out seconds)) return false;
            var item = new ActiveMitigation { Definition = definition, SourceId = sourceId, TargetId = targetId,
                ExpiresUtc = Timestamp(f[1]).AddSeconds(Math.Max(0, seconds)), IsMine = sourceId != 0 && sourceId == PlayerId };
            lock (gate) active[Key(targetId, statusId)] = item;
            return true;
        }

        private bool ParseStatusRemove(string[] f)
        {
            uint statusId, targetId;
            if (f.Length < 8 || !Hex(f[2], out statusId) || !Hex(f[7], out targetId)) return false;
            lock (gate) return active.Remove(Key(targetId, statusId));
        }

        private bool ParseDamage(string[] f)
        {
            uint sourceId, targetId; long damage;
            if (f.Length < 10 || PlayerId == 0 || !Hex(f[2], out sourceId) || !Hex(f[6], out targetId) || targetId != PlayerId) return false;
            var pair = FindDamagePair(f);
            if (pair < 0 || !FfxivAmountDecoder.TryDecode(f[pair + 1], out damage) || damage <= 0) return false;
            var now = Timestamp(f[1]);
            List<ActiveMitigation> applied;
            lock (gate)
            {
                foreach (var key in active.Where(x => x.Value.ExpiresUtc <= now).Select(x => x.Key).ToList()) active.Remove(key);
                applied = active.Values.Where(x =>
                    (x.Definition.Scope == MitigationScope.OnPlayer && x.TargetId == PlayerId) ||
                    (x.Definition.Scope == MitigationScope.OnAttacker && x.TargetId == sourceId)).OrderByDescending(x => x.IsMine).ToList();
            }
            var multiplier = applied.Aggregate(1d, (v, x) => v * (1d - x.Definition.Percent / 100d));
            var before = multiplier > 0.001 ? (long)Math.Round(damage / multiplier, MidpointRounding.AwayFromZero) : damage;
            var evt = new DamageFlytextEvent { TimestampUtc = now, ActionName = f[5], Damage = damage,
                EstimatedBeforeMitigation = before, TotalMitigationPercent = (1d - multiplier) * 100d, Mitigations = applied };
            DamageReceived?.Invoke(this, evt);
            return true;
        }

        private static int FindDamagePair(string[] f)
        {
            for (var i = 8; i + 1 < Math.Min(f.Length, 24); i += 2)
            {
                uint flags;
                if (!Hex(f[i], out flags)) continue;
                var effect = flags & 0xFF;
                if (effect == 3 || effect == 5 || effect == 6 || effect == 0x33) return i;
            }
            return -1;
        }

        private static string Key(uint target, uint status) => target.ToString("X8") + ":" + status.ToString("X");
        private static bool Hex(string value, out uint result) => uint.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out result);
        private static DateTime Timestamp(string value)
        {
            DateTime time;
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out time) ? time.ToUniversalTime() : DateTime.UtcNow;
        }
    }
}
