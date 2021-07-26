using System;
using System.Collections.Generic;
using Verse;

namespace CombatExtended.AI
{
    public static class AI_Utility
    {
        private static readonly Dictionary<Pawn, CompTacticalManager> _compTactical = new Dictionary<Pawn, CompTacticalManager>(2048);

        public static CompTacticalManager GetTacticalManager(this Pawn pawn)
        {
            return _compTactical.TryGetValue(pawn, out var comp) ? comp : _compTactical[pawn] = pawn.TryGetComp<CompTacticalManager>();
        }
    }
}
