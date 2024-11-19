using Mono.Unix.Native;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace CombatExtended
{
    public class Building_CIWS_CE : Building_Turret_MultiVerbs
    {
        #region Caching

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            map.GetComponent<TurretTracker>().Register(this);
        }
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Map.GetComponent<TurretTracker>()?.Unregister(this);
            base.DeSpawn(mode);
        }
        #endregion

        public IEnumerable<ThingDef> IgnoredDefsSettings
        {
            get
            {
                Log.WarningOnce("IgnoredDefs not implemented yet", 82469265);
                return Enumerable.Empty<ThingDef>();
            }
        }
        public override void Tick()
        {
            base.Tick();
            if (CurrentTarget.IsValid && CurrentTarget.HasThing)
            {
                this.top.CurRotation = (CurrentTarget.Thing.DrawPos - this.DrawPos).AngleFlat();
            }
        }
    }
}
