using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse.AI;
using Verse;

namespace CombatExtended
{
    internal class AmmoContainerTracker : MapComponent
    {
        public HashSet<Building_AmmoContainerCE> AmmoContainers = new HashSet<Building_AmmoContainerCE>();

        public AmmoContainerTracker(Map map) : base(map)
        {
        }

        public void Register(Building_AmmoContainerCE t)
        {
            if (!AmmoContainers.Contains(t))
            {
                AmmoContainers.Add(t);
            }
        }

        public void Unregister(Building_AmmoContainerCE t)
        {
            if (AmmoContainers.Contains(t))
            {
                AmmoContainers.Remove(t);
            }
        }
        public Thing ClosestCpmtaomer(IntVec3 position, PathEndMode pathEndMode, TraverseParms parms, float maxDist,
                                   Predicate<Thing> validator = null)
        {
            return GenClosest.ClosestThingReachable(
                       position, map, ThingRequest.ForUndefined(), pathEndMode,
                       parms, maxDist, validator, AmmoContainers);
        }
    }
}
