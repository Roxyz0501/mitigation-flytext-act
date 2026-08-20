using System;
using System.Collections.Generic;

namespace MitigationFlytext
{
    public enum MitigationScope { OnPlayer, OnAttacker }

    public sealed class MitigationDefinition
    {
        public MitigationDefinition(uint id, string name, int percent, MitigationScope scope, string abbreviation)
        { StatusId = id; Name = name; Percent = percent; Scope = scope; Abbreviation = abbreviation; }
        public uint StatusId { get; }
        public string Name { get; }
        public int Percent { get; }
        public MitigationScope Scope { get; }
        public string Abbreviation { get; }
    }

    public sealed class ActiveMitigation
    {
        public MitigationDefinition Definition { get; set; }
        public uint SourceId { get; set; }
        public uint TargetId { get; set; }
        public DateTime ExpiresUtc { get; set; }
        public bool IsMine { get; set; }
    }

    public sealed class DamageFlytextEvent
    {
        public DateTime TimestampUtc { get; set; }
        public string ActionName { get; set; }
        public long Damage { get; set; }
        public long EstimatedBeforeMitigation { get; set; }
        public double TotalMitigationPercent { get; set; }
        public IReadOnlyList<ActiveMitigation> Mitigations { get; set; }
    }
}
