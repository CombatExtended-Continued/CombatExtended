using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended.Lasers
{

    public class LaserGun : ThingWithComps, IBeamColorThing, IDrawnWeaponWithRotation
    {
        new public LaserGunDef def => base.def as LaserGunDef ?? LaserGunDef.defaultObj;

        public int BeamColor
        {
            get { return LaserColor.IndexBasedOnThingQuality(beamColorIndex, this); }
            set { beamColorIndex = value; }
        }

        int ticksPreviously = 0;
        public float RotationOffset
        {
            get
            {
                int ticks = Find.TickManager.TicksGame;
                UpdateRotationOffset(ticks - ticksPreviously);
                ticksPreviously = ticks;

                return rotationOffset;
            }
            set
            {
                rotationOffset = value;
                rotationSpeed = 0;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<int>(ref beamColorIndex, "beamColorIndex", -1, false);
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn pawn)
        {
            foreach (FloatMenuOption o in base.GetFloatMenuOptions(pawn))
            {
                if (o != null) yield return o;
            }

            if (!def.supportsColors) yield break;
            /*
            foreach (FloatMenuOption o in LaserColor.GetChangeBeamColorFloatMenuOptions(this, pawn))
            {
                if (o != null) yield return o;
            }
            */
            yield break;
        }

        void UpdateRotationOffset(int ticks)
        {
            if (rotationOffset == 0) return;
            if (ticks <= 0) return;
            if (ticks > 30) ticks = 30;

            if (rotationOffset > 0)
            {
                rotationOffset -= rotationSpeed;
                if (rotationOffset < 0) rotationOffset = 0;
            }
            else if (rotationOffset < 0)
            {
                rotationOffset += rotationSpeed;
                if (rotationOffset > 0) rotationOffset = 0;
            }

            rotationSpeed += ticks * 0.01f;
        }

        private int beamColorIndex = -1;
        private float rotationSpeed = 0;
        private float rotationOffset = 0;
    }
}
