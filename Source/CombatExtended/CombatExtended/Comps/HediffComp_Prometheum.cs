using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class HediffComp_Prometheum : HediffComp
    {
        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Pawn.IsHashIntervalTick(GenTicks.TicksPerRealSecond))
            {
                Fire fire = Pawn.GetAttachment(ThingDefOf.Fire) as Fire;
                if (fire == null && Pawn.Spawned)
                {
                    Pawn.TryAttachFire(parent.Severity * 0.5f);
                }
                else if (fire != null)
                {
                    fire.fireSize = Mathf.Min(fire.fireSize + parent.Severity * 0.5f, 1.75f);  // Clamped at max fire size
                }
            }
        }
    }
}
