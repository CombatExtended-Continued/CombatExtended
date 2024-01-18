using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VanillaPsycastsExpanded;
using Verse;

namespace CombatExtended.Compatibility
{
    public class VanillaPsycastExpanded : IPatch
    {
        const string ModName = "Vanilla Psycasts Expanded";
        public bool CanInstall()
        {
            Log.Message("Vanilla Psycasts Expanded loaded: " + ModLister.HasActiveModWithName(ModName));
            return ModLister.HasActiveModWithName(ModName);
        }

        public IEnumerable<string> GetCompatList()
        {
            yield break;
        }
        public void Install()
        {
            BlockerRegistry.RegisterImpactSomethingCallback(ImpactSomething);
            BlockerRegistry.RegisterBeforeCollideWithCallback(BeforeCollideWith);
            BlockerRegistry.RegisterCheckForCollisionBetweenCallback(AOE_CheckIntercept);
            BlockerRegistry.RegisterShieldZonesCallback(ShieldZones);
            BlockerRegistry.RegisterUnsuppresableFromCallback(Unsuppresable);
        }
        private static Dictionary<Map, IEnumerable<IEnumerable<IntVec3>>> shieldZones;
        private static int shieldZonesCacheTick = -1;
        private static IEnumerable<IEnumerable<IntVec3>> ShieldZones(Thing thing)
        {
            IEnumerable<IEnumerable<IntVec3>> result = null;
            var currentTick = GenTicks.TicksGame;
            if (shieldZonesCacheTick != currentTick)
            {
                shieldZonesCacheTick = currentTick;
                shieldZones = new Dictionary<Map, IEnumerable<IEnumerable<IntVec3>>>();
            }
            if (!shieldZones.TryGetValue(thing.Map, out result))
            {
                result = thing.Map.listerThings.ThingsInGroup(ThingRequestGroup.Pawn).Cast<Pawn>().SelectMany(x => x.health.hediffSet.hediffs).Where(x => x is Hediff_Overshield).Select(x => { var ho = x as Hediff_Overshield; return GenRadial.RadialCellsAround(ho.pawn.Position, ho.OverlaySize, true); }).ToList();
                shieldZones.Add(thing.Map, result);
            }
            return result;
        }

        private static bool Unsuppresable(Pawn pawn, IntVec3 origin) => pawn.health.hediffSet.hediffs.Any(x => x.GetType() == typeof(Hediff_Overshield));

        private static bool BeforeCollideWith(ProjectileCE projectile, Thing collideWith)
        {
            if (collideWith is Pawn pawn)
            {
                var interceptor = pawn.health.hediffSet.hediffs.FirstOrDefault(x => x.GetType() == typeof(Hediff_Overshield)) as Hediff_Overshield;
                if (interceptor != null)
                {
                    Vector3 exactPos;
                    if (CE_Utility.IntersectionPoint(projectile.OriginIV3.ToVector3(), projectile.ExactPosition, interceptor.pawn.TrueCenter(), interceptor.OverlaySize, out var sect))
                    {
                        exactPos = sect.OrderBy(x => (projectile.OriginIV3.ToVector3() - x).sqrMagnitude).First();
                    }
                    else
                    {
                        Log.Error("Intersection points not found");
                        exactPos = interceptor.pawn.TrueCenter();
                    }
                    OnIntercepted(interceptor, projectile, exactPos);
                    return true;
                }
            }
            return false;
        }
        private static bool ImpactSomething(ProjectileCE projectile, Thing launcher)
        {
            foreach (var interceptor in projectile.Map.thingGrid.ThingsListAt(projectile.ExactPosition.ToIntVec3()).OfType<Pawn>())
            {
                if (BeforeCollideWith(projectile, interceptor))
                {
                    return true;
                }
            }
            return false;
        }

        public static IEnumerable<(Vector3, Action)> AOE_CheckIntercept(ProjectileCE projectile, Vector3 from, Vector3 newExactPos)
        {
            List<(Vector3, Action)> result = new List<(Vector3, Action)>();
            var def = projectile.def;
            if (projectile.def.projectile.flyOverhead)
            {
                return result;
            }
            foreach (var hediff in projectile.Map.listerThings.ThingsInGroup(ThingRequestGroup.Pawn).Cast<Pawn>()
                .SelectMany(x => x.health.hediffSet.hediffs)
                .Where(x => x is Hediff_Overshield && x.GetType() != typeof(Hediff_Overshield)))
            {
                var interceptor = hediff as Hediff_Overshield;
                Vector3 shieldPosition = interceptor.pawn.Position.ToVector3Shifted().Yto0();
                float radius = interceptor.OverlaySize;
                if (CE_Utility.IntersectionPoint(from.Yto0(), newExactPos.Yto0(), shieldPosition, radius, out Vector3[] sect, false, map: projectile.Map))
                {
                    //OnIntercepted(interceptor, projectile, sect);
                    var exactPosition = sect.OrderBy(x => (projectile.OriginIV3.ToVector3() - x).sqrMagnitude).First();
                    result.Add((exactPosition, () => OnIntercepted(hediff, projectile, exactPosition)));
                }
            }
            return result;
        }

        private static void OnIntercepted(object hediff, ProjectileCE projectile, Vector3 exactPosition)
        {
            if (!(hediff is Hediff_Overshield interceptor))
            {
                return;
            }
            projectile.ExactPosition = exactPosition;
            new Traverse(interceptor).Field("lastInterceptAngle").SetValue(exactPosition.AngleToFlat(interceptor.pawn.TrueCenter()));
            new Traverse(interceptor).Field("lastInterceptTicks").SetValue(Find.TickManager.TicksGame);
            new Traverse(interceptor).Field("drawInterceptCone").SetValue(true);

            FleckMakerCE.ThrowPsycastShieldFleck(exactPosition, projectile.Map, 0.35f);
            projectile.InterceptProjectile(interceptor, projectile.ExactPosition, true);
        }
    }
}
