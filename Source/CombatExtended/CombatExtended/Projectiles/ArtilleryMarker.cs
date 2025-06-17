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

        public override string Label
        {
            get
            {
                return string.Concat("CE_MarkedForArtillery".Translate(), " ", ((int)(lifetimeTicks / GenTicks.TicksPerRealSecond)).ToString(), " ", "LetterSecond".Translate());
            }
        }

        public override string InspectStringAddon
        {
            get
            {
                return string.Concat("CE_MarkedForArtillery".Translate(), " ", ((int)(lifetimeTicks / GenTicks.TicksPerRealSecond)).ToString(), " ", "LetterSecond".Translate());
            }
        }

        public override TipSignal GetTooltip()
        {
            string text = string.Concat(this.LabelCap,
                "\n\n", "CE_ArtilleryTarget_MarkerHoverHeader".Translate(),
                "\n\n", CE_StatDefOf.SightsEfficiency.LabelCap, ": ", sightsEfficiency,
                "\n", CE_StatDefOf.AimingAccuracy.LabelCap, ": ", aimingAccuracy);

            return new TipSignal(text);
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

        public override void TickInterval(int delta)
        {
            lifetimeTicks -= delta;
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
