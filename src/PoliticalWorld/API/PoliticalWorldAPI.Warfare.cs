using System.Collections.Generic;

namespace Lous12.PoliticalWorld
{
    public static partial class PoliticalWorldAPI
    {
        public enum WarStartSource
        {
            Unknown = 0,
            AI = 1,
            Player = 2,
            PoliticalWorld = 3,
            Addon = 4,
            CivilWar = 5
        }

        /// <summary>
        /// API 1.15 warfare facade. TryDeclareWar goes through Political
        /// World's diplomatic interception; ForceWar deliberately bypasses it.
        /// </summary>
        public static class Warfare
        {
            public static bool IsAtWar(Kingdom kingdom)
            {
                return Main.ScenarioBridge.IsKingdomAtWar(kingdom);
            }

            public static bool AreAtWar(Kingdom first, Kingdom second)
            {
                return Main.ScenarioBridge.AreKingdomsAtWar(first, second);
            }

            public static List<Kingdom> GetEnemies(Kingdom kingdom)
            {
                return Main.ScenarioBridge.GetWarEnemies(kingdom);
            }

            public static int GetWarExhaustion(Kingdom kingdom)
            {
                return Main.ScenarioBridge.GetWarExhaustion(kingdom);
            }

            public static bool TryDeclareWar(
                Kingdom attacker,
                Kingdom defender,
                object warType = null
            )
            {
                return Main.ScenarioBridge.TryDeclareWar(
                    attacker,
                    defender,
                    warType,
                    false
                );
            }

            public static bool ForceWar(
                Kingdom attacker,
                Kingdom defender,
                object warType = null
            )
            {
                return Main.ScenarioBridge.TryDeclareWar(
                    attacker,
                    defender,
                    warType,
                    true
                );
            }
        }
    }
}
