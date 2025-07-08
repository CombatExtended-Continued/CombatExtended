using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended;
public class HediffComp_Prometheum : HediffComp
{
    private const float InternalFireDamage = 2;

    public override void CompPostTickInterval(ref float severityAdjustment, int delta)
    {
        base.CompPostTickInterval(ref severityAdjustment, delta);

        if (Pawn.IsHashIntervalTick(GenTicks.TicksPerRealSecond, delta))
        {
            if (Pawn.Position.GetThingList(Pawn.Map).Any(x => x.def == ThingDefOf.Filth_FireFoam))
            {
                //clear prometheum-soaked hediff
                severityAdjustment = -1000;
                return;
            }
            Fire fire = Pawn.GetAttachment(ThingDefOf.Fire) as Fire;
            if (fire == null && Pawn.Spawned)
            {
                Pawn.TryAttachFire(parent.Severity * 0.5f * delta, null);
            }
            else if (fire != null)
            {
                fire.fireSize = Mathf.Min(fire.fireSize + parent.Severity * 0.5f * delta, 1.75f);  // Clamped at max fire size
            }

            // Apply to internal parts
            if (Pawn.def.race.IsMechanoid)
            {
                var internalPart = Pawn.health.hediffSet.GetNotMissingParts(BodyPartHeight.Undefined, BodyPartDepth.Inside).RandomElement();
                if (internalPart == null)
                {
                    return;
                }
                Pawn.TakeDamage(new DamageInfo(CE_DamageDefOf.Flame_Secondary, InternalFireDamage * Pawn.BodySize * parent.Severity * delta, 0, -1, null,
                                               internalPart));
            }
        }
    }
}
