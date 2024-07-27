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
    public class CompCIWSTarget : ThingComp
    {
        protected static Dictionary<Map, List<Thing>> CIWSTargets = new Dictionary<Map, List<Thing>>();
        public static IEnumerable<Thing> Targets(Map map)
        {
            CIWSTargets.TryGetValue(map, out var targets);
            return targets ?? Enumerable.Empty<Thing>();
        }

        public CompProperties_CIWSTarget Props => props as CompProperties_CIWSTarget;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (parent.Map == null)
            {
                return;
            }
            if (!CIWSTargets.TryGetValue(parent.Map, out var targetList))
            {
                CIWSTargets[parent.Map] = targetList = new List<Thing>();
            }
            targetList.Add(parent);
        }
        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            if (CIWSTargets.TryGetValue(map, out var targetList))
            {
                targetList.Remove(parent);
                if (targetList.Count <= 0)
                {
                    CIWSTargets.Remove(map);
                }
            }
        }
        public virtual void OnImpact(Projectile projectile, DamageInfo dinfo)
        {
            parent.Position = projectile.Position;
            if (parent is IThingHolder pod)
            {
                var containedThings = Utils.ContainedThings(pod).ToList();

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
            if (!Props.impacted.NullOrUndefined())
            {
                Props.impacted.PlayOneShot(new TargetInfo(parent.DrawPos.ToIntVec3(), parent.Map));
            }
            parent.Destroy(DestroyMode.Vanish);
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
                GenPlace.TryPlaceThing(thing, position, map, ThingPlaceMode.Near, out thing2, delegate (Thing placedThing, int count)
                {
                    if (Find.TickManager.TicksGame < 1200 && TutorSystem.TutorialMode && placedThing.def.category == ThingCategory.Item)
                    {
                        Find.TutorialState.AddStartingItem(placedThing);
                    }
                }, null, rot);
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
        public virtual IntVec3 CalculatePointForPreemptiveFire(ThingDef projectile, Vector3 source, int tickOffset = 0)
        {
            return parent.CalculatePointForPreemptiveFire(projectile, source, tickOffset);
        }

    }
    public class CompProperties_CIWSTarget : CompProperties
    {
        public CompProperties_CIWSTarget()
        {
            compClass = typeof(CompCIWSTarget);
        }
        public SoundDef impacted;
        public bool alwaysIntercept;
    }
}
