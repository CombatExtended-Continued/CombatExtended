using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    class Verb_MarkForArtillery : Verb_LaunchProjectileCE
    {
        public override void WarmupComplete()
        {
            base.WarmupComplete();
            if (ShooterPawn != null && ShooterPawn.skills != null)
            {
                ShooterPawn.skills.Learn(SkillDefOf.Shooting, 200);
            }
        }

        protected override bool TryCastShot()
        {
            ArtilleryMarker marker = ThingMaker.MakeThing(ThingDef.Named(ArtilleryMarker.MarkerDef)) as ArtilleryMarker;
            ShiftVecReport report = ShiftVecReportFor(currentTarget);
            marker.aimEfficiency = report.aimEfficiency;
            marker.aimingAccuracy = report.aimingAccuracy;
            marker.lightingShift = report.lightingShift;
            marker.weatherShift = report.weatherShift;

            GenSpawn.Spawn(marker, this.currentTarget.Cell, caster.Map);

            // Check for something to attach marker to
            if (this.currentTarget.HasThing)
            {
                CompAttachBase comp = this.currentTarget.Thing.TryGetComp<CompAttachBase>();
                if (comp != null)
                {
                    marker.AttachTo(this.currentTarget.Thing);
                }
            }
            return true;
        }
    }
}
