using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse.Sound;
using Verse;

namespace CombatExtended
{
    public class CompCIWSImpactHandler_Skyfaller : CompCIWSImpactHandler
    {
        public override void OnImpact(ProjectileCE projectile, DamageInfo dinfo)
        {
            if (parent is IThingHolder pod)
            {
                var containedThings = pod.ContainedThings().ToList();

                foreach (var thing in containedThings)
                {
                    if (thing is Building)
                    {
                        var leavingList = new List<Thing>();
                        GenLeaving.DoLeavingsFor(thing, parent.Map, DestroyMode.KillFinalize, CellRect.CenteredOn(parent.Position, thing.def.size), listOfLeavingsOut: leavingList);
                        continue;
                    }
                    TryDropThing(thing, projectile.Map, projectile.Position);
                    if (thing is Pawn pawn)
                    {
                        pawn.TakeDamage(dinfo);
                        if (!pawn.Dead)
                        {
                            pawn.Kill(dinfo);
                        }
                    }
                    else
                    {
                        thing.HitPoints = Rand.RangeInclusive(1, thing.MaxHitPoints);
                    }

                }
            }
            base.OnImpact(projectile, dinfo);

        }
        private Thing TryDropThing(Thing thing, Map map, IntVec3 position)
        {
            var contents = (parent as IActiveDropPod)?.Contents;
            Rot4 rot = (contents?.setRotation != null) ? contents.setRotation.Value : Rot4.North;
            if (contents?.moveItemsAsideBeforeSpawning ?? false)
            {
                GenSpawn.CheckMoveItemsAside(parent.Position, rot, thing.def, map);
            }
            Thing thing2;
            if (contents?.spawnWipeMode == null)
            {
                GenPlace.TryPlaceThing(thing, position, map, ThingPlaceMode.Near, out thing2, null, null, rot);
            }
            else if (contents?.setRotation != null)
            {
                thing2 = GenSpawn.Spawn(thing, position, map, contents.setRotation.Value, contents.spawnWipeMode.Value, false, false);
            }
            else
            {
                thing2 = GenSpawn.Spawn(thing, position, map, contents.spawnWipeMode.Value);
            }
            Pawn pawn = thing2 as Pawn;
            if (pawn != null)
            {
                if (pawn.RaceProps.Humanlike)
                {
                    TaleRecorder.RecordTale(TaleDefOf.LandedInPod, new object[]
                    {
                            pawn
                    });
                }
            }
            return thing2;
        }
    }
}
