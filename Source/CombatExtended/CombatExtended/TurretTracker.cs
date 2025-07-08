using System;
using System.Collections.Generic;
using System.Text;
using Verse;
using RimWorld;
using Verse.AI;

namespace CombatExtended;
public class TurretTracker : MapComponent
{
    public HashSet<Building_Turret> Turrets = new HashSet<Building_Turret>();
    public HashSet<Building_CIWS_CE> CIWS = new HashSet<Building_CIWS_CE>();

    public TurretTracker(Map map) : base(map)
    {
    }

    public void Register(Building_Turret t)
    {
        if (!Turrets.Contains(t))
        {
            Turrets.Add(t);
        }
    }
    public void Register(Building_CIWS_CE ciws)
    {
        if (!CIWS.Contains(ciws))
        {
            CIWS.Add(ciws);
        }
    }
    public void Unregister(Building_Turret t)
    {
        if (Turrets.Contains(t))
        {
            Turrets.Remove(t);
        }
    }
    public void Unregister(Building_CIWS_CE ciws)
    {
        if (CIWS.Contains(ciws))
        {
            CIWS.Remove(ciws);
        }
    }

    // Returns the closest turret to `position` on the which matches the criteria set in `validator`
    public Thing ClosestTurret(IntVec3 position, PathEndMode pathEndMode, TraverseParms parms, float maxDist,
                               Predicate<Thing> validator = null)
    {
        return GenClosest.ClosestThingReachable(
                   position, map, ThingRequest.ForUndefined(), pathEndMode,
                   parms, maxDist, validator, Turrets);
    }
}
