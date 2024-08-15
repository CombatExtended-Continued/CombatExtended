using Mono.Unix.Native;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace CombatExtended
{
    internal class ProjectileCE_CIWS : ProjectileCE
    {
        #region Caching
        protected static Dictionary<Map, List<ProjectileCE_CIWS>> CIWSProjectiles = new Dictionary<Map, List<ProjectileCE_CIWS>>();
        public static IEnumerable<ProjectileCE_CIWS> ProjectilesAt(Map map)
        {
            CIWSProjectiles.TryGetValue(map, out var targets);
            return targets ?? Enumerable.Empty<ProjectileCE_CIWS>();
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (map == null)
            {
                return;
            }
            if (!CIWSProjectiles.TryGetValue(map, out var targetList))
            {
                CIWSProjectiles[map] = targetList = new List<ProjectileCE_CIWS>();
            }
            targetList.Add(this);
        }
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Map map = Map;
            base.DeSpawn(mode);
            if (CIWSProjectiles.TryGetValue(map, out var targetList))
            {
                targetList.Remove(this);
                if (targetList.Count <= 0)
                {
                    CIWSProjectiles.Remove(map);
                }
            }
        }
        #endregion

        public virtual float CollideDistance => (def.projectile as ProjectilePropertiesCE)?.collideDistance ?? 1f;
        public virtual float ImpactChance => (def.projectile as ProjectilePropertiesCE)?.impactChance ?? 1f;
        public override void Tick()
        {
            ticksToImpact++; //do not allow it hit zero
            base.Tick();
            TryCollideWith(intendedTargetThing);
        }
        protected override bool CheckForCollisionBetween()
        {
            return false;
        }
    }
}
