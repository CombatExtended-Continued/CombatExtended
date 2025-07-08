using System;
using RimWorld.Planet;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace CombatExtended.WorldObjects;
public static class WorldCompUtility
{
    public static IEnumerable<IWorldCompCE> GetCompsCE(this WorldObject worldObject)
    {
        for (int i = 0; i < worldObject.comps.Count; i++)
        {
            if (worldObject.comps[i] is IWorldCompCE throttleable)
            {
                yield return throttleable;
            }
        }
    }
    public static WorldObjectDamageWorker GetWorldObjectDamageWorker(this ThingDef shellDef) => (shellDef?.GetProjectile()?.projectile as ProjectilePropertiesCE)?.shellingProps?.Worker ?? new WorldObjectDamageWorker();
}

