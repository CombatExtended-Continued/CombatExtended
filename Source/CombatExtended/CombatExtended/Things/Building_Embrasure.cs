using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace CombatExtended
{
    public class Building_Embrasure : Building_Door
    {
        public override bool PawnCanOpen(Pawn p)
        {
            // True for all pawns that are hostile to embrasure owner and have ranged attack
            return p.HostileTo(Faction.OfPlayer)
                && p.equipment != null && p.equipment.Primary != null && !p.equipment.Primary.def.Verbs.NullOrEmpty() && p.equipment.Primary.def.Verbs.Any(v => !v.MeleeRange);
        }
    }
}
