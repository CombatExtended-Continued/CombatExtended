using System;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class ProjectileCE_Flare : ProjectileCE_Explosive
    {
        private const float DISTSHIFT_MIN = -2;
        private const float DISTSHIFT_MAX = 5;

        private float _originToTargetDis = -1;
        private float OriginToTargetDis
        {
            get
            {
                if (_originToTargetDis == -1)
                {
                    _originToTargetDis = Vector3.Distance(intendedTarget.CenterVector3, new Vector3(origin.x, 0, origin.y));
                    _originToTargetDis += Rand.Range(DISTSHIFT_MIN, DISTSHIFT_MAX);
                }
                return _originToTargetDis;
            }
        }
        private float CurrentDistance
        {
            get
            {
                return Vector3.Distance(intendedTarget.CenterVector3, ExactPosition);
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (OriginToTargetDis < CurrentDistance)
            {
                Impact(null);
            }
        }

        public override void Impact(Thing hitThing)
        {
            landed = true;
            Flare flare;
            flare = (Flare)ThingMaker.MakeThing(CE_ThingDefOf.Flare, null);
            flare.StartingAltitude = Height;
            flare.DrawMode = Flare.FlareDrawMode.FlyOver;
            flare.Position = Position;
            flare.SpawnSetup(Map, false);
            base.Impact(null);
        }
    }
}
