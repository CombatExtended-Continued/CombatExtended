using System;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class ProjectileCE_Flare : ProjectileCE_Explosive
    {
        private const float FLYOVER_FLARING_CHANCE = 0.2f;
        private const float FLYOVER_FLARING_HEIGHT = Flare.DEFAULT_FLYOVER_START_ALT;

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
                if (Height <= FLYOVER_FLARING_HEIGHT && Rand.Chance(FLYOVER_FLARING_CHANCE))
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
            landed = true;
            Flare flare;
            flare = (Flare)ThingMaker.MakeThing(CE_ThingDefOf.Flare, null);
            flare.DrawMode = Flare.FlareDrawMode.FlyOver;
            flare.StartingAltitude = Height;
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
