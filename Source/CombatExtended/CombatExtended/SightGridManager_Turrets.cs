using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CombatExtended.Utilities;
using RimWorld;
using RimWorld.BaseGen;
using UnityEngine;
using Verse;
using Verse.AI;

namespace CombatExtended
{
    public class SightGridUpdater_Turrets : SightGridManager<Building_TurretGunCE>
    {
        public SightGridUpdater_Turrets(SightTracker tracker, int bucketCount = 30, int minUpdateInterval = 200, int maxUpdateInterval = 400)
            : base(tracker, bucketCount, minUpdateInterval, maxUpdateInterval)
        {            
        }             

        protected override int GetSightRange(IThingSightRecord record)
        {            
            return (int) (record.thing.CurrentEffectiveVerb?.EffectiveRange ?? -1);
        }

        protected override bool Skip(IThingSightRecord record)
        {
            return !record.thing.Active || (record.thing.IsMannable && !(record.thing.MannableComp?.MannedNow ?? false));
        }

        protected override IEnumerable<Building_TurretGunCE> ThingsInRange(IntVec3 position, float range)
        {            
            yield break;
        }
    }
}

