using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class CompFleckEmitterSmooth : ThingComp
    {
        public int ticksSinceLastEmitted;

        public Vector3 lastPos;

        public int tickToCutoff;

        private CompProperties_FleckEmitterSmooth Props => (CompProperties_FleckEmitterSmooth)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
        }

        private bool IsOn
        {
            get
            {
                if (!parent.Spawned)
                {
                    return false;
                }
                CompPowerTrader comp = parent.GetComp<CompPowerTrader>();
                if (comp != null && !comp.PowerOn)
                {
                    return false;
                }
                CompSendSignalOnCountdown comp2 = parent.GetComp<CompSendSignalOnCountdown>();
                if (comp2 != null && comp2.ticksLeft <= 0)
                {
                    return false;
                }
                if (parent is Building_MusicalInstrument building_MusicalInstrument && !building_MusicalInstrument.IsBeingPlayed)
                {
                    return false;
                }
                CompInitiatable comp3 = parent.GetComp<CompInitiatable>();
                if (comp3 != null && !comp3.Initiated)
                {
                    return false;
                }
                CompLoudspeaker comp4 = parent.GetComp<CompLoudspeaker>();
                if (comp4 != null && !comp4.Active)
                {
                    return false;
                }
                CompHackable comp5 = parent.GetComp<CompHackable>();
                if (comp5 != null && comp5.IsHacked)
                {
                    return false;
                }
                if (parent is Building_Crate building_Crate && !building_Crate.HasAnyContents)
                {
                    return false;
                }
                if (Props.cutoffTickRange.max > 0 && tickToCutoff > Props.cutoffTickRange.max)
                {
                    return false;
                }
                return true;
            }
        }

        float cutoffScaleOffset()
        {
            if (Props.cutoffTickRange.max < 0 || tickToCutoff < Props.cutoffTickRange.min) { return 1; }
            return ((float)Props.cutoffTickRange.max - tickToCutoff) / (Props.cutoffTickRange.max - Props.cutoffTickRange.min);
        }

        public override void CompTick()
        {
            if (!IsOn)
            {
                return;
            }
            if (lastPos == Vector3.zero && parent.Spawned)
            {
                lastPos = parent.DrawPos;
            }
            if (Props.emissionInterval != -1)
            {
                if (ticksSinceLastEmitted >= Props.emissionInterval)
                {
                    Emit();
                    ticksSinceLastEmitted = 0;
                }
                else
                {
                    ticksSinceLastEmitted++;
                }
            }
            tickToCutoff++;
        }

        private void Emit()
        {
            Vector3 diff = Vector3.zero;
            for (int i = 0; i < Props.flecksPerTick; i++)
            {
                FleckCreationData dataStatic = FleckMaker.GetDataStatic(parent.DrawPos + Props.offset + diff, parent.MapHeld, Props.fleck, Props.scale.RandomInRange * cutoffScaleOffset());
                dataStatic.rotation = Props.rotation.RandomInRange;
                for (int j = 0; j < Props.thickness; j++)
                {
                    parent.MapHeld.flecks.CreateFleck(dataStatic);
                }
                diff += (lastPos - parent.DrawPos) / Props.flecksPerTick;
            }
            lastPos = parent.DrawPos;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ticksSinceLastEmitted, "ticksSinceLastEmitted", 0);
            Scribe_Values.Look(ref lastPos, "lastPos");
            Scribe_Values.Look(ref tickToCutoff, "tickToCutoff");
        }
    }

    public class CompProperties_FleckEmitterSmooth : CompProperties_FleckEmitter
    {
        public int flecksPerTick = 1;

        public FloatRange rotation = new FloatRange(0, 360);

        public FloatRange scale = new FloatRange(1, 1);

        public int thickness = 1;

        public IntRange cutoffTickRange = new IntRange(-1, -1);

        public CompProperties_FleckEmitterSmooth()
        {
            compClass = typeof(CompFleckEmitterSmooth);
        }
    }
}
