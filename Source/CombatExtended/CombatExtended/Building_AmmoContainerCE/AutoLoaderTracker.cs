using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse.AI;
using Verse;

namespace CombatExtended;
internal class AutoLoaderTracker : MapComponent
{
    public HashSet<Building_AutoloaderCE> AutoLoaders = new HashSet<Building_AutoloaderCE>();

    public AutoLoaderTracker(Map map) : base(map)
    {
    }

    public void Register(Building_AutoloaderCE t)
    {
        if (!AutoLoaders.Contains(t))
        {
            AutoLoaders.Add(t);
        }
    }

    public void Unregister(Building_AutoloaderCE t)
    {
        if (AutoLoaders.Contains(t))
        {
            AutoLoaders.Remove(t);
        }
    }
    public Thing ClosestCpmtaomer(IntVec3 position, PathEndMode pathEndMode, TraverseParms parms, float maxDist,
                               Predicate<Thing> validator = null)
    {
        return GenClosest.ClosestThingReachable(
                   position, map, ThingRequest.ForUndefined(), pathEndMode,
                   parms, maxDist, validator, AutoLoaders);
    }
}
