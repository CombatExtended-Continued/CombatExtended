﻿using System;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class ProjectileCE_Flare : ProjectileCE_Explosive
    {
        private const float FLYOVER_FLARING_CHANCE = 0.2f;
        private float FLYOVER_FLARING_HEIGHT = 30;
        private ThingDef FlareThingDefOf = CE_ThingDefOf.Flare;
        FlareModExtensionCE ModExtension;

        private bool decentStarted = false;

        private float _originToTargetDis = -1;
        private float OriginToTargetDist
        {
            get
            {
                if (_originToTargetDis == -1)
                {
                    _originToTargetDis = Vector3.Distance(intendedTarget.CenterVector3, new Vector3(origin.x, 0, origin.y));
                }
                return _originToTargetDis;
            }
        }
        private float CurrentDist
        {
            get
            {
                return Vector3.Distance(intendedTarget.CenterVector3, ExactPosition.Yto0());
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (decentStarted)
            {
                if (ExactPosition.y <= FLYOVER_FLARING_HEIGHT && Rand.Chance(FLYOVER_FLARING_CHANCE))
                {
                    Impact(null);
                }
            }
            else if (OriginToTargetDist / 2 > CurrentDist)
            {
                decentStarted = true;
            }
        }

        public override void Impact(Thing hitThing)
        {
            if (this.def.HasModExtension<FlareModExtensionCE>())
            {
                ModExtension = def.GetModExtension<FlareModExtensionCE>();
                FLYOVER_FLARING_HEIGHT = ModExtension.FlyoverStartAltitude;
                FlareThingDefOf = ModExtension.FlareThingDef;
            }

            landed = true;
            Flare flare;
            flare = (Flare)ThingMaker.MakeThing(FlareThingDefOf, null);
            flare.modExtension = ModExtension;
            flare.DrawMode = Flare.FlareDrawMode.FlyOver;
            flare.StartingAltitude = ExactPosition.y;
            flare.Position = Position;
            flare.SpawnSetup(Map, false);
            base.Impact(null);
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref decentStarted, "decentStarted", false);
        }
    }
}
