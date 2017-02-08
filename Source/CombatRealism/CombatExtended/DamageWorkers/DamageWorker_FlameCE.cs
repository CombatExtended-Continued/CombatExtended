using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace CombatExtended
{
    // Cloned from vanilla DamageWorker_Flame, only change is inheritance from DamageWorker_AddInjuryCE so we can have the new armor system apply to this as well
    public class DamageWorker_FlameCE : DamageWorker_AddInjuryCE
    {
        public override float Apply(DamageInfo dinfo, Thing victim)
        {
            if (!dinfo.InstantOldInjury)
            {
                victim.TryAttachFire(Rand.Range(0.15f, 0.25f));
            }
            Pawn pawn = victim as Pawn;
            if (pawn != null && pawn.Faction == Faction.OfPlayer)
            {
                Find.TickManager.slower.SignalForceNormalSpeedShort();
            }
            return base.Apply(dinfo, victim);
        }

        public override void ExplosionAffectCell(Explosion explosion, IntVec3 c, List<Thing> damagedThings, bool canThrowMotes)
        {
            base.ExplosionAffectCell(explosion, c, damagedThings, canThrowMotes);
            if (def == DamageDefOf.Flame && c.IsValid && explosion.Map != null)
            {
                FireUtility.TryStartFireIn(c, explosion.Map, Rand.Range(0.2f, 0.6f));
            }
        }
    }
}
