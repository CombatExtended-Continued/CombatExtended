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

        public void Install()
        {

            BlockerRegistry.RegisterImpactSomethingCallback(ImpactSomething); //temp commented
            BlockerRegistry.RegisterBeforeCollideWithCallback(BeforeCollideWith);
            BlockerRegistry.RegisterCheckForCollisionCallback(Hediff_Overshield_InterceptCheck);
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
                    OnIntercepted(interceptor, projectile, null);
                    return true;
                }
            }
            return false;
        }
        private static bool ImpactSomething(ProjectileCE projectile, Thing launcher)
        {
            return Hediff_Overshield_InterceptCheck(projectile, projectile.ExactPosition.ToIntVec3(), launcher);
        }
        public static bool Hediff_Overshield_InterceptCheck(ProjectileCE projectile, IntVec3 cell, Thing launcher)
        {
            foreach (var interceptor in projectile.Map.thingGrid.ThingsListAt(cell).OfType<Pawn>()
                .SelectMany(x => x.health.hediffSet.hediffs)
                .Where(x => x.GetType() == typeof(Hediff_Overshield)).Cast<Hediff_Overshield>())
            {
                var def = projectile.def;
                var result = interceptor.pawn != launcher && (interceptor.pawn.Position == cell);
                if (result)
                {
                    OnIntercepted(interceptor, projectile, null);
                    return result;
                }
            }
            return false;
        }
        public static bool AOE_CheckIntercept(ProjectileCE projectile, Vector3 from, Vector3 newExactPos)
        {
            var def = projectile.def;
            if (projectile.def.projectile.flyOverhead)
            {
                return false;
            }
            foreach (var interceptor in projectile.Map.listerThings.ThingsInGroup(ThingRequestGroup.Pawn).Cast<Pawn>()
                .SelectMany(x => x.health.hediffSet.hediffs)
                .Where(x => x is Hediff_Overshield && x.GetType() != typeof(Hediff_Overshield)).Cast<Hediff_Overshield>())
            {
                Vector3 shieldPosition = interceptor.pawn.Position.ToVector3Shifted().Yto0();
                float radius = interceptor.OverlaySize;
                if (CE_Utility.IntersectionPoint(from.Yto0(), newExactPos.Yto0(), shieldPosition, radius, out Vector3[] sect, false, map: projectile.Map))
                {
                    OnIntercepted(interceptor, projectile, sect);
                    return true;
                }
            }

            return false;
        }

        private static void OnIntercepted(Hediff hediff, ProjectileCE projectile, Vector3[] sect)
        {
            if (!(hediff is Hediff_Overshield interceptor))
            {
                return;
            }
            var newExactPos = projectile.ExactPosition;
            if (sect == null)
            {
                CE_Utility.IntersectionPoint(projectile.OriginIV3.ToVector3(), projectile.ExactPosition, interceptor.pawn.Position.ToVector3(), interceptor.OverlaySize, out sect);
            }
            var exactPosition = sect.OrderBy(x => (projectile.OriginIV3.ToVector3() - x).sqrMagnitude).First();
            projectile.ExactPosition = exactPosition;
            new Traverse(interceptor).Field("lastInterceptAngle").SetValue(exactPosition.AngleToFlat(interceptor.pawn.TrueCenter()));
            new Traverse(interceptor).Field("lastInterceptTicks").SetValue(Find.TickManager.TicksGame);
            new Traverse(interceptor).Field("drawInterceptCone").SetValue(true);

            FleckMakerCE.ThrowPsycastShieldFleck(exactPosition, projectile.Map, 0.35f);
            projectile.InterceptProjectile(interceptor, projectile.ExactPosition, true);
        }
    }
}
