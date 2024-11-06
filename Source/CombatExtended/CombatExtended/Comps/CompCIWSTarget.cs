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
    /// <summary>
    /// Base class for third-party mods compatibility (if their skyfallers\projectiles needs custom targeting logic)
    /// </summary>
    public abstract class CompCIWSTarget : ThingComp
    {
        protected static Dictionary<Map, List<Thing>> CIWSTargets = new Dictionary<Map, List<Thing>>();
        public static IEnumerable<Thing> Targets(Map map)
        {
            CIWSTargets.TryGetValue(map, out var targets);
            return targets ?? Enumerable.Empty<Thing>();
        }
        public static IEnumerable<Thing> Targets<T>(Map map) where T : CompCIWSTarget
        {
            return Targets(map).Where(x=>x.HasComp<T>());
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
        public abstract bool IsFriendlyTo(Thing thing);
        public abstract bool CalculatePointForPreemptiveFire(ThingDef projectile, Vector3 source, out Vector3 result, int tickOffset = 0);
    }
    public class CompProperties_CIWSTarget : CompProperties
    {
        public CompProperties_CIWSTarget(){}
        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (var item in base.ConfigErrors(parentDef))
            {
                yield return item;
            }
            if (!compClass.IsAssignableFrom(typeof(CompCIWSTarget)))
            {
                yield return "compClass must be the heir to class " + nameof(CompCIWSTarget);
            }
        }
        public bool alwaysIntercept;
    }
}
