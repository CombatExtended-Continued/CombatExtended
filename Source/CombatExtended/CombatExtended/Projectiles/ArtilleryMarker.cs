using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    class ArtilleryMarker : AttachableThing
    {
        public const string MarkerDef = "ArtilleryMarker";

        public Pawn caster; 
        public float aimingAccuracy = 1f;
        public float sightsEfficiency = 1f;
        public float lightingShift = 0f;
        public float weatherShift = 0f;

        private int lifetimeTicks = 1800;

        public override string InspectStringAddon
        {
            get { return "CE_MarkedForArtillery".Translate() + " " + ((int)(lifetimeTicks / 60)).ToString() + " s"; }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref caster, "caster");
            Scribe_Values.Look(ref this.aimingAccuracy, "aimingAccuracy");
            Scribe_Values.Look(ref this.sightsEfficiency, "sightsEfficiency");
            Scribe_Values.Look(ref this.lightingShift, "lightingShift");
            Scribe_Values.Look(ref this.weatherShift, "weatherShift");
            Scribe_Values.Look(ref this.lifetimeTicks, "lifetimeTicks");
        }

        public override void Tick()
        {
            lifetimeTicks--;
            if (lifetimeTicks <= 0)
            {
                this.Destroy();
            }
        }

        public override void AttachTo(Thing parent)
        {
            if (parent != null)
            {
                CompAttachBase comp = parent.TryGetComp<CompAttachBase>();
                if (comp != null)
                {
                    if (parent.HasAttachment(ThingDef.Named(ArtilleryMarker.MarkerDef)))
                    {
                        ArtilleryMarker oldMarker = (ArtilleryMarker)parent.GetAttachment(ThingDef.Named(ArtilleryMarker.MarkerDef));
                        oldMarker.Destroy();
                    }
                }
            }
            base.AttachTo(parent);
        }

    }
}
