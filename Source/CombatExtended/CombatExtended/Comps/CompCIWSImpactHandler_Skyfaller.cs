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

            base.OnImpact(projectile, dinfo);
            if (!parent.Destroyed)
            {
                if (parent is IThingHolder pod)
                {
                    var pawns = pod.ContainedThings().OfType<Pawn>().ToList();
                    if (pawns.Any())
                    {
                        pawns.RandomElement().TakeDamage(dinfo);
                    }
                }
            }
        }
        protected override void OnDestroying(DamageInfo dinfo)
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
                    TryDropThing(thing, parent.Map, parent.DrawPos.ToIntVec3());
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
            base.OnDestroying(dinfo);
        }
        private Thing TryDropThing(Thing thingToDrop, Map map, IntVec3 position)
        {
            var contents = (parent as IActiveDropPod)?.Contents;
            Rot4 rot = (contents?.setRotation != null) ? contents.setRotation.Value : Rot4.North;
            if (contents?.moveItemsAsideBeforeSpawning ?? false)
            {
                GenSpawn.CheckMoveItemsAside(parent.Position, rot, thingToDrop.def, map);
            }
            Thing dropedThing;
            if (contents?.spawnWipeMode == null)
            {
                GenPlace.TryPlaceThing(thingToDrop, position, map, ThingPlaceMode.Near, out dropedThing, null, null, rot);
            }
            else if (contents?.setRotation != null)
            {
                dropedThing = GenSpawn.Spawn(thingToDrop, position, map, contents.setRotation.Value, contents.spawnWipeMode.Value, false, false);
            }
            else
            {
                dropedThing = GenSpawn.Spawn(thingToDrop, position, map, contents.spawnWipeMode.Value);
            }
            if (dropedThing is Pawn pawn)
            {
                if (pawn.RaceProps.Humanlike)
                {
                    TaleRecorder.RecordTale(TaleDefOf.LandedInPod, new object[]
                    {
                            pawn
                    });
                }
            }
            return dropedThing;
        }
    }
}
