using System.Collections.Generic;

namespace MitigationFlytext
{
    public static class MitigationCatalog
    {
        private static readonly Dictionary<uint, MitigationDefinition> Items = Build();
        public static bool TryGet(uint id, out MitigationDefinition value) => Items.TryGetValue(id, out value);
        public static IEnumerable<MitigationDefinition> All => Items.Values;

        private static Dictionary<uint, MitigationDefinition> Build()
        {
            var d = new Dictionary<uint, MitigationDefinition>();
            Add(d, 1191, "Rampart", 20, MitigationScope.OnPlayer, "RM");
            Add(d, 74, "Sentinel", 30, MitigationScope.OnPlayer, "SE");
            Add(d, 89, "Vengeance", 30, MitigationScope.OnPlayer, "VE");
            Add(d, 747, "Shadow Wall", 30, MitigationScope.OnPlayer, "SW");
            Add(d, 1834, "Nebula", 30, MitigationScope.OnPlayer, "NE");
            Add(d, 3832, "Damnation", 40, MitigationScope.OnPlayer, "DA");
            Add(d, 3838, "Great Nebula", 40, MitigationScope.OnPlayer, "GN");
            Add(d, 1193, "Reprisal", 10, MitigationScope.OnAttacker, "RP");
            Add(d, 1195, "Feint", 10, MitigationScope.OnAttacker, "FE");
            Add(d, 1203, "Addle", 10, MitigationScope.OnAttacker, "AD");
            Add(d, 860, "Dismantle", 10, MitigationScope.OnAttacker, "DI");
            Add(d, 1934, "Troubadour", 10, MitigationScope.OnPlayer, "TR");
            Add(d, 1951, "Tactician", 10, MitigationScope.OnPlayer, "TA");
            Add(d, 1826, "Shield Samba", 10, MitigationScope.OnPlayer, "SS");
            Add(d, 1872, "Temperance", 10, MitigationScope.OnPlayer, "TE");
            Add(d, 299, "Sacred Soil", 10, MitigationScope.OnPlayer, "SO");
            Add(d, 849, "Collective Unconscious", 10, MitigationScope.OnPlayer, "CU");
            Add(d, 2618, "Kerachole", 10, MitigationScope.OnPlayer, "KE");
            Add(d, 2711, "Desperate Measures", 10, MitigationScope.OnPlayer, "DM");
            Add(d, 1839, "Heart of Light", 10, MitigationScope.OnPlayer, "HL");
            Add(d, 1894, "Dark Missionary", 10, MitigationScope.OnPlayer, "DM");
            Add(d, 2708, "Aquaveil", 15, MitigationScope.OnPlayer, "AQ");
            Add(d, 2717, "Exaltation", 10, MitigationScope.OnPlayer, "EA");
            Add(d, 2619, "Taurochole", 10, MitigationScope.OnPlayer, "TC");
            Add(d, 3003, "Holos", 10, MitigationScope.OnPlayer, "HO");
            Add(d, 2682, "Oblation", 10, MitigationScope.OnPlayer, "OB");
            Add(d, 2675, "Knight's Resolve", 15, MitigationScope.OnPlayer, "KR");
            Add(d, 2680, "Stem the Tide", 10, MitigationScope.OnPlayer, "ST", true);
            Add(d, 2684, "Clarity of Corundum", 15, MitigationScope.OnPlayer, "CC");
            Add(d, 1362, "Divine Veil", 0, MitigationScope.OnPlayer, "DV", true);
            Add(d, 1457, "Shake It Off", 0, MitigationScope.OnPlayer, "SI", true);
            Add(d, 1178, "The Blackest Night", 0, MitigationScope.OnPlayer, "BN", true);
            Add(d, 1898, "Brutal Shell", 0, MitigationScope.OnPlayer, "BS", true);
            Add(d, 1218, "Divine Benison", 0, MitigationScope.OnPlayer, "DB", true);
            Add(d, 297, "Galvanize", 0, MitigationScope.OnPlayer, "GA", true);
            Add(d, 1918, "Catalyze", 0, MitigationScope.OnPlayer, "CA", true);
            Add(d, 1917, "Seraphic Veil", 0, MitigationScope.OnPlayer, "SV", true);
            Add(d, 1887, "The Spire", 0, MitigationScope.OnPlayer, "SP", true);
            Add(d, 1921, "Neutral Sect", 0, MitigationScope.OnPlayer, "NS", true);
            Add(d, 1889, "Celestial Intersection", 0, MitigationScope.OnPlayer, "CI", true);
            Add(d, 3365, "Holosakos", 0, MitigationScope.OnPlayer, "HS", true);
            Add(d, 2607, "Eukrasian Diagnosis", 0, MitigationScope.OnPlayer, "ED", true);
            Add(d, 2608, "Differential Diagnosis", 0, MitigationScope.OnPlayer, "DD", true);
            Add(d, 2609, "Eukrasian Prognosis", 0, MitigationScope.OnPlayer, "EP", true);
            Add(d, 2612, "Haima", 0, MitigationScope.OnPlayer, "HA", true);
            Add(d, 2613, "Panhaima", 0, MitigationScope.OnPlayer, "PH", true);
            Add(d, 2702, "Radiant Aegis", 0, MitigationScope.OnPlayer, "RA", true);
            Add(d, 168, "Manaward", 0, MitigationScope.OnPlayer, "MW", true);
            Add(d, 488, "Shade Shift", 0, MitigationScope.OnPlayer, "SH", true);
            Add(d, 2596, "Crest of Time Borrowed", 0, MitigationScope.OnPlayer, "CT", true);
            Add(d, 3686, "Tempera Coat", 0, MitigationScope.OnPlayer, "TC", true);
            Add(d, 3687, "Tempera Grassa", 0, MitigationScope.OnPlayer, "TG", true);
            Add(d, 2697, "Improvised Finish", 0, MitigationScope.OnPlayer, "IF", true);
            return d;
        }

        private static void Add(Dictionary<uint, MitigationDefinition> d, uint id, string name, int percent, MitigationScope scope, string abbreviation, bool hasBarrier = false)
            => d[id] = new MitigationDefinition(id, name, percent, scope, abbreviation, hasBarrier);
    }
}
