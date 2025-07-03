using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace CombatExtended
{
    public class CompProjectileFleck : ThingComp
    {
        public Vector3 lastPos;

        public int age => projectile.FlightTicks;

        ProjectileCE projectile;

        private CompProperties_ProjectileFleck Props => (CompProperties_ProjectileFleck)props;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            projectile = parent as ProjectileCE;
        }

        private bool IsOn
        {
            get
            {
                if (!parent.Spawned)
                {
                    return false;
                }
                if (projectile == null)
                {
                    return false;
                }
                return true;
            }
        }

        public override void CompTickInterval(int delta)
        {
            if (!IsOn)
            {
                return;
            }
            if (lastPos == Vector3.zero && parent.Spawned)
            {
                lastPos = parent.DrawPos;
            }
            Emit();
        }

        private void Emit()
        {
            Vector3 delta = lastPos - parent.DrawPos;
            foreach (ProjectileFleckDataCE data in Props.FleckDatas)
            {
                if ((data.cutoffTickRange.max < 0 || age < data.cutoffTickRange.max)
                    && (data.startDelayTick < 0 || age > data.startDelayTick)
                    && data.shouldEmit(age))
                {
                    float scaleoffset = 0;
                    Vector3 diff = Vector3.zero;
                    for (int i = 0; i < data.emissionAmount; i++)
                    {
                        FleckCreationData dataStatic = FleckMaker.GetDataStatic(parent.DrawPos - data.originOffset.RandomInRange * delta + diff, parent.MapHeld, data.fleck, (data.scale.RandomInRange + scaleoffset) * data.cutoffScaleOffset(age));
                        dataStatic.rotation = data.rotation.RandomInRange;
                        for (int j = 0; j < data.flecksPerEmission; j++)
                        {
                            parent.MapHeld.flecks.CreateFleck(dataStatic);
                        }
                        if (data.emissionAmount > 1)
                        {
                            diff += delta / data.emissionAmount;
                            scaleoffset += data.scaleOffsetInternal;
                        }
                    }
                }
            }
            lastPos = parent.DrawPos;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref lastPos, "lastPos");
        }
    }

    public class CompProperties_ProjectileFleck : CompProperties
    {
        public List<ProjectileFleckDataCE> FleckDatas;

        public CompProperties_ProjectileFleck()
        {
            compClass = typeof(CompProjectileFleck);
        }
    }

    public class ProjectileFleckDataCE
    {
        public FleckDef fleck;

        float emissionsPerTick = 1;

        public int flecksPerEmission = 1;

        public FloatRange rotation = new FloatRange(0, 360);

        public FloatRange scale = new FloatRange(1, 1);

        public IntRange cutoffTickRange = new IntRange(-1, -1);

        public int startDelayTick = 1;

        public FloatRange originOffset = new FloatRange(0.7f, 0.7f);

        public int emissionAmount => emissionsPerTick > 1 ? (int)emissionsPerTick : 1;

        [Obsolete("This property is obsolete. Use the FloatRange originOffset instead.", true)]
        public float originOffsetInternal => 1 - originOffset.RandomInRange; //deprecated field, left to not break mod xml
        public float scaleOffsetInternal => fleck.growthRate / (emissionsPerTick * 60);

        public float cutoffScaleOffset(int age)
        {
            if (cutoffTickRange.max < 0 || age < cutoffTickRange.min) { return 1; }
            return ((float)cutoffTickRange.max - age) / (cutoffTickRange.max - cutoffTickRange.min);
        }

        public bool shouldEmit(int age)
        {
            if (emissionsPerTick >= 1)
            {
                return true;
            }
            return age % (int)(1 / emissionsPerTick) == 0;
        }
    }
}
